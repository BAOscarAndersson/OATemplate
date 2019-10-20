using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.Mvc;
using OATemplate.Models;
using System.Xml;
using System.Configuration;

namespace OATemplate.Controllers
{
    public class TemplateController : Controller
    {
        public ViewResult Index()
        {
            Template template = new Template();
            template.publications = new Dictionary<int, string>();

            //The program works with a few paths which are stored in Web.config.
            var appSettings = ConfigurationManager.AppSettings;
            string inPath = appSettings["In"];
            
            //Get the different publications from their associated XML file from a folder.
            string[] allFilesInDir = Directory.GetFiles(inPath, @"*.XML");
            int i = 0;
            while (i < allFilesInDir.Length)
            {
                template.publications.Add(i, Path.GetFileName(allFilesInDir[i]));
                i++;
            } 

            //Inital view just used to select a publication.
            return View(template);
        }
        
        [HttpPost]
        public ViewResult PublicationSelected(FormCollection values)
        {
            Template template = new Template();

            var appSettings = ConfigurationManager.AppSettings;
            string inPath = appSettings["In"];
            string savedTemplates = appSettings["Templates"];
            
            //The publication that was selected by the user is read into the model.
            template.selectedPublication = values["Publication"];

            //Read the associated XML file of the publication and figure out how many broadsheet pages there are in it.
            string xmlLocation = Path.Combine(inPath, template.selectedPublication);
            XmlDocument publicationXML = new XmlDocument();
            publicationXML.Load(xmlLocation);
            XmlNodeList pagesList = publicationXML.SelectNodes("/Planning/productionRuns/productionRun/plates/plate");
            template.numberOfPages = pagesList.Count / 4;

            //Template files are saved in subdirectories of savedTemplates based on their plate count.
            string[] allFilesInDir = Directory.GetFiles(Path.Combine(savedTemplates, pagesList.Count.ToString()), @"*.xml");

            //Get the file names and put them into the model.
            template.savedTemplates = new string[allFilesInDir.Length];
            for(int i = 0; i<allFilesInDir.Length; i++)
            {
                template.savedTemplates[i] = Path.GetFileName(allFilesInDir[i]);
            }

            //View to select a template or make a new one.
            return View(template);
        }

        [HttpPost]
        public ViewResult TemplateSelected(FormCollection values)
        {
            Template template = new Template();

            var appSettings = ConfigurationManager.AppSettings;
            string savedTemplatesPath = appSettings["Templates"];
            string pressXMLPathAndFile = appSettings["PressXML"];
            template.selectedTemplate = values["Template"];
            template.selectedPublication = values["selctPub"];
            int tempResult = Int32.Parse(values["NrPages"]);
            template.numberOfPages = tempResult;

            //The selected template is loaded into a DOM.
            XmlDocument savedTemplateXML = new XmlDocument();
            XmlDocument pressXML = new XmlDocument();
            pressXML.Load(pressXMLPathAndFile);
            savedTemplateXML.Load(Path.Combine(savedTemplatesPath, (template.numberOfPages * 4).ToString(), template.selectedTemplate));

            foreach (XmlNode aTower in savedTemplateXML.SelectSingleNode("/Press").ChildNodes)
                foreach (XmlNode aCylinder in savedTemplateXML.SelectSingleNode("/Press/" + aTower.Name).ChildNodes)
                    foreach (XmlNode aHalfCylinder in savedTemplateXML.SelectSingleNode("/Press/" + aTower.Name + "/" + aCylinder.Name).ChildNodes)
                        foreach (XmlNode aSection in savedTemplateXML.SelectSingleNode("/Press/" + aTower.Name + "/" + aCylinder.Name + "/" + aHalfCylinder.Name).ChildNodes)
                            foreach (XmlNode aPage in savedTemplateXML.SelectSingleNode("/Press/" + aTower.Name + "/" + aCylinder.Name + "/" + aHalfCylinder.Name + "/" + aSection.Name).ChildNodes)
                                pressXML.SelectSingleNode("/Press/" + aTower.Name + "/" + aCylinder.Name + "/" + aHalfCylinder.Name + "/" + aSection.Name + "/" + aPage.Name).InnerText = aPage.InnerText;

            template.Press = pressXML;

            //Fills in the form with the page numbers from the model.
            return View(template);
        }

        [HttpPost]
        public ViewResult TemplateCommitted(FormCollection values)
        {
            Template template = new Template();

            var appSettings = ConfigurationManager.AppSettings;

            //The layout of the press is saved as a XML file.
            template.Press = new XmlDocument();
            string pressXMLPathAndFile = appSettings["PressXML"];
            template.Press.Load(pressXMLPathAndFile);

            //Update the XML file with the information from the model.

            //Send the XML to the prepress-system and set the view to reflect the success or failure of this operation.
            return View();
        }
    }
}
