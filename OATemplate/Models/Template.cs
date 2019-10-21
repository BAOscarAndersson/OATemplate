using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace OATemplate.Models
{
    public class Template
    {
        public string selectedPublication;
        public Dictionary<int, string> publications;
        public int numberOfPages;
        public string[] savedTemplates;
        public string selectedTemplate;
        public Dictionary<string, string> pagesAndLocation;
        public string failureOrSuccess;

        public XmlDocument Press;
    }
}