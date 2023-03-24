using System;
using Microsoft.AspNetCore.Identity;

namespace webApi.Identity
{
    public class User: IdentityUser
    {
        public string CompanyName { get; set; }  
        public string CompanyAddress { get; set; }
        public bool Kvkk { get; set; }
    }        
    
}