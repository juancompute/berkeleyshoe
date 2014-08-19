﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BerkeleyEntities;
using eBay.Service.Core.Soap;
using System.IO;
using System.Net;
using eBay.Service.Call;
using eBay.Service.Core.Sdk;
using EbayServices.Mappers;
using EbayServices.Services;
using System.Xml;
using System.Data;

namespace BerkeleyEntities.Ebay
{
    public delegate void PublishingResultHandler(ResultArgs e);

    public class Publisher
    {
        public const string FORMAT_FIXEDPRICE = "FixedPriceItem";
        public const string FORMAT_AUCTION = "Chinese";
        public const string STATUS_ACTIVE = "Active";

        private berkeleyEntities _dataContext;
        private ListingMapper _listingMapper;
        private EbayMarketplace _marketplace;
        private ListingSyncService _listingSyncService;

        public Publisher(berkeleyEntities dataContext,  EbayMarketplace marketplace)
        {
            _marketplace = marketplace;
            _dataContext = dataContext;

            _listingSyncService = new ListingSyncService(marketplace.ID);
            _listingMapper = new ListingMapper(_dataContext, _marketplace);
        }

        public event PublishingResultHandler Result;

        public void Publish()
        {
            var modified = _dataContext.ObjectStateManager.GetObjectStateEntries(EntityState.Modified).Select(p => p.Entity).OfType<EbayListing>().ToList();
            var created = _dataContext.ObjectStateManager.GetObjectStateEntries(EntityState.Added).Select(p => p.Entity).OfType<EbayListing>().ToList();

            foreach (var listing in created)
            {
                try
                {
                    PublishListing(listing);
                    this.Result(new ResultArgs() { Listing = listing, Message = string.Empty, IsError = false });
                }
                catch (Exception e)
                {
                    Revert(listing);
                    this.Result(new ResultArgs() { Listing = listing, Message = e.Message, IsError = true });
                }
            }

            foreach (var listing in modified)
            {
                try
                {
                    if (listing.ListingItems.All(p => p.Quantity == 0))
                    {
                        EndListing(listing);
                    }
                    else
                    {
                        ReviseListing(listing);
                    }

                    this.Result(new ResultArgs() { Listing = listing, Message = string.Empty, IsError = false });
                }
                catch (Exception e)
                {
                    Revert(listing);
                    this.Result(new ResultArgs() { Listing = listing, Message = e.Message, IsError = true });
                }
            } 

        }

        public void Revert(EbayListing listing)
        {
            foreach (EbayListingItem listingItem in listing.ListingItems.ToList())
            {
                _dataContext.EbayListingItems.Detach(listingItem);
            }

            foreach (EbayPictureUrlRelation relation in listing.Relations.ToList())
            {
                _dataContext.EbayPictureUrlRelations.Detach(relation);
            }

            _dataContext.EbayListings.Detach(listing);
        }

        private void EndListing(EbayListing listing)
        {
            EndItemRequestType request = new EndItemRequestType();
            request.ItemID = listing.Code;
            request.EndingReasonSpecified = true;
            request.EndingReason = EndReasonCodeType.NotAvailable;
                           
            EndItemCall call = new EndItemCall(_marketplace.GetApiContext());

            EndItemResponseType response = call.ExecuteRequest(request) as EndItemResponseType;
        
            listing.Status = "Completed";

            if (response.EndTimeSpecified)
            {
                listing.EndTime = response.EndTime;
            }
        }

        private void PublishListing(EbayListing listing)
        {
            var pendingUrls = listing.Relations.Select(p => p.PictureServiceUrl).Where(p => p.Url == null);

            foreach (var urlData in pendingUrls)
            {
                urlData.Url = UploadToEPS(urlData.Path);
                urlData.TimeUploaded = DateTime.UtcNow;
            }     

            if ((bool)listing.IsVariation)
            {
                AddFixedPriceItemRequestType request = new AddFixedPriceItemRequestType();
                request.Item = _listingMapper.Map(listing);

                AddFixedPriceItemCall call = new AddFixedPriceItemCall(_marketplace.GetApiContext());
                AddFixedPriceItemResponseType response = call.ExecuteRequest(request) as AddFixedPriceItemResponseType;
                listing.LastSyncTime = DateTime.UtcNow;
                listing.Code = response.ItemID;
                listing.StartTime = response.StartTime;
                listing.EndTime = response.EndTime;
                listing.Status = "Active";
            }
            else
            {
                AddItemRequestType request = new AddItemRequestType();
                request.Item = _listingMapper.Map(listing);

                AddItemCall call = new AddItemCall(_marketplace.GetApiContext());
                AddItemResponseType response = call.ExecuteRequest(request) as AddItemResponseType;
                listing.LastSyncTime = DateTime.UtcNow;
                listing.Code = response.ItemID;
                listing.StartTime = response.StartTime;
                listing.EndTime = response.EndTime;
                listing.Status = "Active";
            }
        }

        private void ReviseListing(EbayListing listing)
        {
            if ((bool)listing.IsVariation)
            {
                ReviseFixedPriceItemRequestType request = new ReviseFixedPriceItemRequestType();
                request.Item = _listingMapper.Map(listing);

                ReviseFixedPriceItemCall call = new ReviseFixedPriceItemCall(_marketplace.GetApiContext());
                ReviseFixedPriceItemResponseType response = call.ExecuteRequest(request) as ReviseFixedPriceItemResponseType;
                listing.LastSyncTime = DateTime.UtcNow;
                
            }
            else
            {
                ReviseItemRequestType request = new ReviseItemRequestType();
                request.Item = _listingMapper.Map(listing);

                
                ReviseItemCall call = new ReviseItemCall(_marketplace.GetApiContext());
                ReviseItemResponseType response = call.ExecuteRequest(request) as ReviseItemResponseType;
                listing.LastSyncTime = DateTime.UtcNow;
                
            }
        }

        private string UploadToEPS(string path)
        {
            string boundary = "MIME_boundary";
            string CRLF = "\r\n";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.ebay.com/ws/api.dll");


            //Add the request headers
            request.Headers.Add("X-EBAY-API-COMPATIBILITY-LEVEL", "515");
            request.Headers.Add("X-EBAY-API-DEV-NAME", "2ec7ed97-e88e-4868-aeb8-9be51f107a46"); //use your devid
            request.Headers.Add("X-EBAY-API-APP-NAME", "Mecalzoc-a8e6-4947-ac5b-49be352cd2f4"); //use your appid
            request.Headers.Add("X-EBAY-API-CERT-NAME", "Mecalzoc-a8e6-4947-ac5b-49be352cd2f4"); //use your certid
            request.Headers.Add("X-EBAY-API-SITEID", "0");
            request.Headers.Add("X-EBAY-API-DETAIL-LEVEL", "0");
            request.Headers.Add("X-EBAY-API-CALL-NAME", "UploadSiteHostedPictures");
            request.ContentType = "multipart/form-data; boundary=" + boundary;

            request.ProtocolVersion = HttpVersion.Version10;

            request.ContentType = "multipart/form-data; boundary=" + boundary;

            request.Method = "POST";

            string payload1 = "--" + boundary + CRLF +
                "Content-Disposition: form-data; name=document" + CRLF +
                "Content-Type: text/xml; charset=\"UTF-8\"" + CRLF + CRLF +
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<UploadSiteHostedPicturesRequest xmlns=\"urn:ebay:apis:eBLBaseComponents\">" +
                "<PictureSet>Supersize</PictureSet>" +
                "<RequesterCredentials><eBayAuthToken>" + _marketplace.Token + "</eBayAuthToken></RequesterCredentials>" +
                "</UploadSiteHostedPicturesRequest>" +
                CRLF + "--" + boundary + CRLF +
                "Content-Disposition: form-data; name=image; filename=image" + CRLF +
                "Content-Type: application/octet-stream" + CRLF +
                "Content-Transfer-Encoding: binary" + CRLF + CRLF;

            string payload3 = CRLF + "--" + boundary + "--" + CRLF;

            byte[] postDataBytes1 = Encoding.ASCII.GetBytes(payload1);
            byte[] postDataBytes2 = Encoding.ASCII.GetBytes(payload3);
            byte[] image = null;

            using (Stream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fileStream.Seek(0, SeekOrigin.Begin);

                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    image = reader.ReadBytes((int)fileStream.Length);
                }
            }

            int length = postDataBytes1.Length + image.Length + postDataBytes2.Length;

            request.ContentLength = length;

            using (Stream stream = request.GetRequestStream())
            {
                byte[] bytes = postDataBytes1.Concat(image).Concat(postDataBytes2).ToArray();

                stream.Write(bytes, 0, length);
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string output = null;

            using (StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                output = sr.ReadToEnd();
            }

            XmlDocument xmlResponse = new XmlDocument();

            xmlResponse.LoadXml(output);

            XmlNodeList list = xmlResponse.GetElementsByTagName("FullURL", "urn:ebay:apis:eBLBaseComponents");

            return list[0].InnerText;
        }
    }

    public class ResultArgs
    {
        public bool IsError { get; set; }

        public string Message {get; set;}

        public EbayListing Listing {get; set;}
    }
}
