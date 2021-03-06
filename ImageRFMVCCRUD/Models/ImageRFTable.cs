//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ImageRFMVCCRUD.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public partial class ImageRFTable
    {
        [Key()]
        public int ImageRFId { get; set; }
        public string Requestor { get; set; }
        public string InquiryCategory { get; set; }
        public string TeamName { get; set; }
        public string ReviewRequestTitle { get; set; }
        public string Details { get; set; }
        public System.DateTime DateRequested { get; set; }
        public string AliasesToCC { get; set; }
        public Nullable<System.DateTime> ProductReleaseDate { get; set; }
        public string TargetMarkets { get; set; }
        public string NumberOfImages { get; set; }
        public string ListOfFilesUploaded { get; set; }
        public string UploadedFolderPath { get; set; }
        public string ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public string EmailAddress { get; set; }
    }
}
