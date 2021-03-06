﻿using System.Collections.Generic;
using Watchdog.Forms.Util;
using Watchdog.Persistence;

namespace Watchdog.Entities
{
    public class Rating : Persistable
    {
        private static readonly Rating defaultValue = new Rating();
        [PersistableField(0)]
        [TableHeader("RATING-CODE", 400)]
        public string RatingCode { get; set; }
        [PersistableField(1)]
        [TableHeader("WERT")]
        // Rating class according to concordance table of federal authorities
        public double RatingNumericValue { get; set; }
        public double Index { get; set; }
        [PersistableField(2)]
        public RatingAgency Agency { get; set; }

        public Rating()
        {
        }

        public string GetTableName()
        {
            return "wdt_ratings";
        }

        public double GetIndex()
        {
            return Index;
        }

        public void SetIndex(double index)
        {
            Index = index;
        }

        public static Rating GetDefaultValue()
        {
            return defaultValue;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            return obj is Rating rating &&
                   RatingCode == rating.RatingCode &&
                   RatingNumericValue == rating.RatingNumericValue &&
                   Index == rating.Index &&
                   Agency == rating.Agency;
        }

        public override int GetHashCode()
        {
            int hashCode = 1239053354;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(RatingCode);
            hashCode = hashCode * -1521134295 + RatingNumericValue.GetHashCode();
            hashCode = hashCode * -1521134295 + Index.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<RatingAgency>.Default.GetHashCode(Agency);
            return hashCode;
        }

        public string GetShortName()
        {
            return "rat";
        }
    }
}
