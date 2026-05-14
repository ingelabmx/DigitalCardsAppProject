using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DigitalCardsApp.Models
{
    public class ClientDetails
    {
        public int UsID { get; set; }
        public string UsName { get; set; }
        public string UsPassword { get; set; }
        public string UsFirstName { get; set; }
        public string UsLastName { get; set; }
        public string UsEmail { get; set; }
        public int UsRole {  get; set; }
    }
}