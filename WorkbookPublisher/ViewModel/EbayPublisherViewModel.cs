﻿using BerkeleyEntities;
using BerkeleyEntities.Ebay;
using Microsoft.TeamFoundation.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace WorkbookPublisher
{
    enum StatusCode { PENDING, PENDING_CREATION, PENDING_UPDATE, COMPLETED }

    public class EbayPublisherViewModel 
    {
        private PublishCommand _publishCommand;
        private RelayCommand _readEntriesCommand;
        private RelayCommand _fixErrorsCommand;

        private berkeleyEntities _dataContext = new berkeleyEntities();
        private ObservableCollection<EbayEntry> _entries = new ObservableCollection<EbayEntry>();
        private ExcelWorkbook _workbook;
        private EbayMarketplace _marketplace;


        public EbayPublisherViewModel(ExcelWorkbook workbook, int marketplaceID)
        {
            _marketplace = _dataContext.EbayMarketplaces.Single(p => p.ID == marketplaceID);
            _workbook = workbook;

            this.Entries = CollectionViewSource.GetDefaultView(_entries);
            this.Entries.Filter = p => ((EbayEntry)p).Status.Equals("error");
        }

        public ICollectionView Entries { get; set; }

        public string Header 
        {
            get { return _marketplace.Code; }
        }

        public string Progress
        {
            get
            {
                if (this.Entries != null)
                {
                    var entries = this.Entries.SourceCollection.OfType<EbayEntry>();

                    string completed = entries.Where(p => p.Status.Equals("completed")).Count().ToString();
                    string waiting = entries.Where(p => p.Status.Equals("waiting")).Count().ToString();
                    string errors = entries.Where(p => p.Status.Equals("error")).Count().ToString();

                    return string.Format("{0} Pending\n{1} Completed\n{2} Errors", waiting, completed, errors);
                }
                else
                {
                    return "0 Pending\n0 Completed\n0 Errors";
                }

            }
        }

        public ICommand PublishCommand 
        {
            get
            {
                if (_publishCommand == null)
                {
                    _publishCommand = new PublishCommand(_marketplace.ID);
                }

                return _publishCommand;
            }
        }

        public ICommand ReadCommand 
        {
            get 
            {
                if (_readEntriesCommand == null)
                {                  
                    _readEntriesCommand = new RelayCommand(ReadEntries);
                }

                return _readEntriesCommand;
            }
        }

        public ICommand FixCommand
        {
            get
            {
                if (_fixErrorsCommand == null)
                {
                    _fixErrorsCommand = new RelayCommand(FixErrors);
                }

                return _fixErrorsCommand;
            }
        }

        private async void ReadEntries(object parameter)
        {
            _entries.Clear();

            var entries = _workbook.ReadEbayEntries(_marketplace.Code + "(errors)");

            if (entries.Count() == 0)
            {
                entries = _workbook.ReadEbayEntries(_marketplace.Code);
            }

            if (entries.Count() > 0)
            {
                await Task.Run(() => UpdateEntries(entries));
            }
            else
            {
                MessageBox.Show("no entries for " + _marketplace.Code);
            }

            foreach (var entry in entries)
            {
                _entries.Add(entry);
            }
        }

        private void FixErrors(object parameter)
        {
            _workbook.PersistEntries(_marketplace.Code + "(errors)", this.Entries.OfType<EbayEntry>().ToList());

        }

        private void UpdateEntries(IEnumerable<EbayEntry> entries)
        {
            foreach (EbayEntry entry in entries)
            {
                Item item = _dataContext.Items.SingleOrDefault(p => p.ItemLookupCode.Equals(entry.Sku));

                entry.Brand = item.SubDescription1;
                entry.ClassName = item.ClassName;

                if (item == null)
                {
                    entry.Message = "sku not found";
                    continue;
                }

                string format = entry.GetFormat();

                if (format == null)
                {
                    entry.Message = "invalid format";
                    continue;
                }

                var listingItems = item.EbayListingItems.Where(p =>
                    p.Listing.MarketplaceID == _marketplace.ID &&
                    p.Listing.Status.Equals(Publisher.STATUS_ACTIVE) &&
                    p.Listing.Format.Equals(format));


                if (listingItems.Count() == 1)
                {
                    EbayListingItem listingItem = listingItems.First();

                    if (listingItem.Quantity == entry.Q)
                    {
                        entry.Completed = true;
                    }
                }
                else if (listingItems.Count() > 1)
                {
                    entry.Message = "duplicate";
                }

                if (entry.Status.Equals("waiting"))
                {
                    if (format.Equals(Publisher.FORMAT_AUCTION) && entry.Q > 1)
                    {
                        entry.Message = "auction max qty is 1";
                    }
                    if (entry.Q > item.QtyAvailable)
                    {
                        entry.Message = "out of stock";
                    }
                    if (item.Department == null)
                    {
                        entry.Message = "department required";
                    }
                    if (entry.Title.Count() > 80)
                    {
                        entry.Message = "title max characters is 80";
                    }
                }

            }
        }
    }

    public class EbayEntry : INotifyPropertyChanged
    {
        private bool _completed = false;
        private List<string> _messages = new List<string>();

        public EbayEntry()
        {
            this.Title = string.Empty;
            this.StartDate = DateTime.UtcNow;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public uint RowIndex { get; set; }
        public string Brand { get; set; }
        public string ClassName { get; set; }
        public string Sku { get; set; }
        public int Q { get; set; }
        public decimal P { get; set; }
        public DateTime StartDate { get; set; }
        public bool StartDateSpecified { get; set; }
        public string Format { get; set; }
        public string Title { get; set; }
        public string Condition { get; set; }
        public string FullDescription { get; set; }

        public bool Completed
        {
            get { return _completed; }
            set
            {
                _completed = value;

                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Status"));
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Progress"));
                }
            }
        }
        public string Message
        {
            get { return string.Join(" | ", _messages); }
            set
            {
                _messages.Add(value);

                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Message"));
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Status"));
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Progress"));
                }
            }
        }

        public string Status 
        {
            get 
            {
                if (this.Completed)
                {
                    return "completed";
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(this.Message))
                    {
                        return "waiting";
                    }
                    else
                    {
                        return "error";
                    }
                }
            }
        }

        public bool IsAuction()
        {
            if (this.Format.Contains("A"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public string GetDuration()
        {
            switch (this.Format)
            {
                case "GTC": return this.Format;

                case "BIN": return "Days_30";

                case "A7": return "Days_7";

                case "A1": return "Days_1";

                case "A3": return "Days_3";

                case "A5": return "Days_5";

                default: return string.Empty;
            }
        }

        public string GetFormat()
        {
            switch (this.Format)
            {
                case "BIN":
                case "GTC":
                    return Publisher.FORMAT_FIXEDPRICE;
                case "A1":
                case "A3":
                case "A5":
                case "A7":
                    return Publisher.FORMAT_AUCTION;
                default:
                    return null;
            }
        }

        public string GetConditionID()
        {
            switch (this.Condition)
            {
                case "NEW": return "1000";
                case "NWB": return "1500";
                case "PRE": return "1750";
                case "NWD": return "3000";

                default: return null;
            }
        }

    }



    public class PublishCommand : ICommand
    {
        private berkeleyEntities _dataContext = new berkeleyEntities();
        private Dictionary<EbayListing, List<EbayEntry>> _targetListings = new Dictionary<EbayListing, List<EbayEntry>>();
        private PictureSetRepository _picSetRepository = new PictureSetRepository();
        private EbayMarketplace _marketplace;
        private Publisher _publisher;
        private bool _canExecute = true;

        
        public event EventHandler CanExecuteChanged;

        public PublishCommand(int marketplaceID)
        {
            _marketplace = _dataContext.EbayMarketplaces.Single(p => p.ID == marketplaceID);

            _publisher = new Publisher(_dataContext, _marketplace);
            _publisher.Result += Publisher_Result;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public void Execute(object parameter)
        {
            var pendingEntries = ((ICollectionView)parameter).SourceCollection.OfType<EbayEntry>().Where(p => p.Status.Equals("waiting"));

            HandleFixedPriceEntries(pendingEntries.Where(p => !p.IsAuction()));

            HandleAuctionEntries(pendingEntries.Where(p => p.IsAuction()));

            Task.Run(() => _publisher.Publish());
        }

        private void Publisher_Result(ResultArgs e)
        {
            foreach (EbayEntry entry in _targetListings[e.Listing])
            {
                entry.Message = e.Message;
                entry.Completed = !e.IsError;
            }
        }

        private void HandleAuctionEntries(IEnumerable<EbayEntry> entries)
        {
            var auctions = _marketplace.Listings.Where(p => p.Format.Equals(Publisher.FORMAT_AUCTION)).Where(p => p.Status.Equals(Publisher.STATUS_ACTIVE)).ToList();

            foreach (EbayEntry entry in entries)
            {
                if (!auctions.Any(p => p.Sku.Equals(entry.Sku)))
                {
                    CreateAuction(entry);
                }
                else
                {
                    entry.Message = "cannot modify auctions";
                }
            }

        }

        private void CreateAuction(EbayEntry entry)
        {
            EbayListing listing = new EbayListing();
            listing.Sku = entry.Sku;
            listing.Marketplace = _marketplace;
            listing.Condition = entry.GetConditionID();
            listing.Duration = entry.GetDuration();
            listing.Format = Publisher.FORMAT_AUCTION;
            listing.FullDescription = entry.FullDescription;
            listing.Title = entry.Title;
            listing.IsVariation = false;

            if (entry.StartDateSpecified)
            {
                listing.StartTime = entry.StartDate;
            }

            EbayListingItem listingItem = new EbayListingItem();
            listingItem.Item = _dataContext.Items.Single(p => p.ItemLookupCode.Equals(entry.Sku));
            listingItem.Listing = listing;
            listingItem.Quantity = entry.Q;
            listingItem.Price = entry.P;

            _targetListings.Add(listing, new List<EbayEntry>() { entry });

            try
            {
                AssignPictures(listing);
            }
            catch (FileNotFoundException e)
            {
                _dataContext.EbayListings.Detach(listing);
                entry.Message = e.Message;
            }
        }

        private void HandleFixedPriceEntries(IEnumerable<EbayEntry> entries)
        {
            var fixedPrice = _marketplace.Listings.Where(p => p.Status.Equals(Publisher.STATUS_ACTIVE) && p.Format.Equals(Publisher.FORMAT_FIXEDPRICE)).ToList();

            foreach (var group in entries.GroupBy(p => p.ClassName))
            {
                List<EbayEntry> pending = new List<EbayEntry>();

                foreach (EbayEntry entry in group)
                {
                    if (fixedPrice.Any(p => p.Sku.Equals(entry.Sku)))
                    {
                        UpdateIndividualListing(fixedPrice.Single(p => p.Sku.Equals(entry.Sku)), entry);
                    }
                    else
                    {
                        pending.Add(entry);
                    }
                }

                if (pending.Count > 0 && fixedPrice.Any(p => p.Sku.Equals(group.Key)))
                {
                    UpdateVariationListing(fixedPrice.Single(p => p.Sku.Equals(group.Key)), pending);
                }
                else if (pending.Count() == 1)
                {
                    CreateIndividualListing(pending.First());
                }
                else if (pending.Count() > 1)
                {
                    CreateVariationListing(pending, group.Key);
                }
            }
        }

        private void UpdateIndividualListing(EbayListing listing, EbayEntry entry)
        {
            EbayListingItem listingItem = listing.ListingItems.First();

            listingItem.Quantity = entry.Q;
            listingItem.Price = entry.P;

            if (!string.IsNullOrWhiteSpace(entry.Title))
            {
                listing.Title = entry.Title;
            }
            if (!string.IsNullOrWhiteSpace(entry.FullDescription))
            {
                listing.FullDescription = entry.FullDescription;
            }

            _targetListings.Add(listing, new List<EbayEntry>() { entry });
        }

        private void CreateIndividualListing(EbayEntry entry)
        {
            EbayListing listing = new EbayListing();
            listing.Sku = entry.Sku;
            listing.Marketplace = _marketplace;
            listing.Condition = entry.GetConditionID();
            listing.Duration = entry.GetDuration();
            listing.Format = Publisher.FORMAT_FIXEDPRICE;
            listing.FullDescription = entry.FullDescription;
            listing.Title = entry.Title;
            listing.IsVariation = false;

            EbayListingItem listingItem = new EbayListingItem();
            listingItem.Item = _dataContext.Items.Single(p => p.ItemLookupCode.Equals(entry.Sku));
            listingItem.Listing = listing;
            listingItem.Quantity = entry.Q;
            listingItem.Price = entry.P;

            try
            {
                AssignPictures(listing);
            }
            catch (FileNotFoundException e)
            {
                entry.Message = e.Message;
            }

            if (listing.Relations.Count < 1)
            {
                entry.Message = "no picture found";
                _publisher.Revert(listing);
            }

            _targetListings.Add(listing, new List<EbayEntry>() { entry });
        }

        private void UpdateVariationListing(EbayListing listing, IEnumerable<EbayEntry> entries)
        {
            if (entries.Any(p => !string.IsNullOrWhiteSpace(p.Title)))
            {
                listing.Title = GetParentTitle(entries.First(p => !string.IsNullOrWhiteSpace(p.Title)));
            }
            if (entries.Any(p => !string.IsNullOrWhiteSpace(p.FullDescription)))
            {
                listing.FullDescription = entries.First(p => !string.IsNullOrWhiteSpace(p.FullDescription)).FullDescription;
            }

            listing.Title = listing.Title;

            foreach (EbayEntry entry in entries)
            {
                Item item = _dataContext.Items.Single(p => p.ItemLookupCode.Equals(entry.Sku));

                EbayListingItem listingItem = listing.ListingItems.SingleOrDefault(p => p.Item.ID == item.ID);

                if (listingItem == null)
                {
                    listingItem = new EbayListingItem() { Item = item, Listing = listing };
                }

                listingItem.Quantity = entry.Q;
                listingItem.Price = entry.P;
            }

            _targetListings.Add(listing, entries.ToList());
        }

        private void CreateVariationListing(IEnumerable<EbayEntry> entries, string className)
        {
            EbayListing listing = new EbayListing();
            listing.Sku = className;
            listing.Marketplace = _marketplace;
            listing.Condition = entries.First(p => !string.IsNullOrWhiteSpace(p.Condition)).GetConditionID();
            listing.Duration = entries.First(p => !string.IsNullOrWhiteSpace(p.Format)).GetDuration();
            listing.Format = Publisher.FORMAT_FIXEDPRICE;
            listing.FullDescription = entries.First(p => !string.IsNullOrWhiteSpace(p.FullDescription)).FullDescription;
            listing.Title = GetParentTitle(entries.First(p => !string.IsNullOrWhiteSpace(p.Title)));
            listing.IsVariation = true;

            foreach (EbayEntry entry in entries)
            {
                EbayListingItem listingItem = new EbayListingItem();
                listingItem.Item = _dataContext.Items.Single(p => p.ItemLookupCode.Equals(entry.Sku));
                listingItem.Listing = listing;
                listingItem.Quantity = entry.Q;
                listingItem.Price = entry.P;
            }

            try
            {
                AssignPictures(listing);
            }
            catch (FileNotFoundException e)
            {
                entries.ToList().ForEach(p => p.Message = e.Message);
            }

            if (listing.Relations.Count < 1)
            {
                entries.ToList().ForEach(p => p.Message = "no picture found");
                _publisher.Revert(listing);
            }

            _targetListings.Add(listing, entries.ToList());
        }

        private void AssignPictures(EbayListing listing)
        {
            string brand = listing.ListingItems.First().Item.SubDescription1;
            var skus = listing.ListingItems.Select(p => p.Item.ItemLookupCode);

            var pics = _picSetRepository.GetPictures(brand, skus.ToList());

            foreach (PictureInfo picInfo in pics)
            {
                var urls = _dataContext.EbayPictureServiceUrls.Where(p => p.LocalName.Equals(picInfo.Name)).ToList();

                EbayPictureServiceUrl url = urls.SingleOrDefault(p => !p.IsExpired() && picInfo.LastModified < p.TimeUploaded);

                if (url == null)
                {
                    url = new EbayPictureServiceUrl();
                    url.LocalName = picInfo.Name;
                    url.Path = picInfo.Path;
                    new EbayPictureUrlRelation() { PictureServiceUrl = url, Listing = listing, CreatedTime = DateTime.UtcNow };
                }
                else
                {
                    if (!listing.Relations.Any(p => p.PictureServiceUrl.ID == url.ID))
                    {
                        new EbayPictureUrlRelation() { PictureServiceUrl = url, Listing = listing, CreatedTime = DateTime.UtcNow };
                    }
                }
            }
        }

        private string GetParentTitle(EbayEntry entry)
        {
            string title = entry.Title;

            Item item = _dataContext.Items.Single(p => p.ItemLookupCode.Equals(entry.Sku));

            var wordsToRemove = item.Attributes.Select(p => p.Key).Concat(item.Attributes.Select(p => p.Value.Value));

            foreach (string word in wordsToRemove)
            {
                title = title.Replace(" " + word + " ", " ");
            }

            return title;
        }
    }
}
