﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BerkeleyEntities;

namespace EbayServices.Mappers
{
    public class ProductMapperFactory 
    {



        public ProductMapper GetProductData(Item item)
        {
            string deptCode = item.Department.code;

            switch (deptCode)
            {
                case "11632":
                case "62107":
                case "53548":
                case "55793":
                case "45333":
                case "53557":
                case "95672":
                case "155202":
                case "155196":
                case "57929":
                case "147285":
                case "11505":
                case "11504":
                case "11501":
                case "53120":
                case "24087":
                case "11498":
                case "15709":
                case "57974":
                    return new ShoesAdapter(item);

                case "11483":
                case "57989":
                    return new PantsAdapter(item);

                default: throw new NotImplementedException(item.Department.Name + " not supported for Ebay");
            }

        }

        public ProductMatrixMapper GetProductMatrixData(string className, IEnumerable<Item> items)
        {
            List<ProductMapper> products = new List<ProductMapper>();

            foreach (Item item in items)
            {
                products.Add(GetProductData(item));
            }

            return new ProductMatrixMapper(products);
        }
    }
}
