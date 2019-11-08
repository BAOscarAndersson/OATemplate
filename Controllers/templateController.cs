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

            //A load of values we've got over here.
            var appSettings = ConfigurationManager.AppSettings;
            string inPath = appSettings["In"];
            string outPath = appSettings["Out"];
            template.selectedPublication = values["selctPub"];
            int tempResult = Int32.Parse(values["NrPages"]);
            template.numberOfPages = tempResult;
            template.selectedTemplate = values["selctTemp"];

            //Moves the pages and their location into an dictionary.
            int i = 0;
            Dictionary<string, string> pagesLocation = new Dictionary<string, string>();
            while (i < values.Count && i < template.numberOfPages * 4)
            {
                string tempKey = values.GetKey(i);
                string tempValue = values.Get(i);
                if (tempValue.Length > 0 && !tempKey.Contains("selct") && !tempKey.Contains("NrPages"))
                    if(!pagesLocation.ContainsKey(tempValue))
                        pagesLocation.Add(tempValue, tempKey);
                
                i++;
            }
            template.pagesAndLocation = pagesLocation;

            //Load the XML for the selected publication into a DOM
            string xmlLocation = Path.Combine(inPath, template.selectedPublication);
            XmlDocument publicationXML = new XmlDocument();
            publicationXML.Load(xmlLocation);
            XmlNode pages = publicationXML.SelectSingleNode("Planning/productionRuns/productionRun/plates");
            XmlNode currentNode = pages;
            currentNode = currentNode.FirstChild;
            int cylinderNumber;
            string cylinderText;
            string topOrBottom;

            if (template.pagesAndLocation.Count * 2 == template.numberOfPages)
            {
                for (int j = 1; j < template.pagesAndLocation.Count + 1; j++)
                {
                    //BLACK
                    currentNode["towerName"].InnerText = pagesLocation[j.ToString()].Substring(0,3);
                    currentNode["locationOnCylinder"].InnerText = pagesLocation[j.ToString()].Substring(8, 1);
                    currentNode["cylinderName"].InnerText = pagesLocation[j.ToString()].Substring(3, 4);
                    currentNode["cylinderSector"].InnerText = "Top";
                    currentNode["cylinderSectorName"].InnerText = "TOP";
                    currentNode = currentNode.NextSibling;
                    currentNode["towerName"].InnerText = pagesLocation[j.ToString()].Substring(0, 3);
                    currentNode["locationOnCylinder"].InnerText = pagesLocation[j.ToString()].Substring(8, 1);
                    currentNode["cylinderName"].InnerText = pagesLocation[j.ToString()].Substring(3, 4);
                    currentNode["cylinderSector"].InnerText = "Bottom";
                    currentNode["cylinderSectorName"].InnerText = "BOTTOM";

                    //CYAN
                    currentNode = currentNode.NextSibling;
                    currentNode["towerName"].InnerText = pagesLocation[j.ToString()].Substring(0, 3);
                    currentNode["locationOnCylinder"].InnerText = pagesLocation[j.ToString()].Substring(8, 1);
                    cylinderNumber = Int32.Parse(pagesLocation[j.ToString()].Substring(6, 1));
                    cylinderNumber -= 6;
                    cylinderText = pagesLocation[j.ToString()].Substring(3, 3) + cylinderNumber.ToString();
                    currentNode["cylinderName"].InnerText = cylinderText;
                    currentNode["cylinderSector"].InnerText = "Top";
                    currentNode["cylinderSectorName"].InnerText = "TOP";
                    currentNode = currentNode.NextSibling;
                    currentNode["towerName"].InnerText = pagesLocation[j.ToString()].Substring(0, 3);
                    currentNode["locationOnCylinder"].InnerText = pagesLocation[j.ToString()].Substring(8, 1);
                    cylinderNumber = Int32.Parse(pagesLocation[j.ToString()].Substring(6, 1));
                    cylinderNumber -= 6;
                    cylinderText = pagesLocation[j.ToString()].Substring(3, 3) + cylinderNumber.ToString();
                    currentNode["cylinderName"].InnerText = cylinderText;
                    currentNode["cylinderSector"].InnerText = "Bottom";
                    currentNode["cylinderSectorName"].InnerText = "BOTTOM";

                    //MAGENTA
                    currentNode = currentNode.NextSibling;
                    currentNode["towerName"].InnerText = pagesLocation[j.ToString()].Substring(0, 3);
                    currentNode["locationOnCylinder"].InnerText = pagesLocation[j.ToString()].Substring(8, 1);
                    cylinderNumber = Int32.Parse(pagesLocation[j.ToString()].Substring(6, 1));
                    cylinderNumber -= 4;
                    cylinderText = pagesLocation[j.ToString()].Substring(3, 3) + cylinderNumber.ToString();
                    currentNode["cylinderName"].InnerText = cylinderText;
                    currentNode["cylinderSector"].InnerText = "Top";
                    currentNode["cylinderSectorName"].InnerText = "TOP";
                    currentNode = currentNode.NextSibling;
                    currentNode["towerName"].InnerText = pagesLocation[j.ToString()].Substring(0, 3);
                    currentNode["locationOnCylinder"].InnerText = pagesLocation[j.ToString()].Substring(8, 1);
                    cylinderNumber = Int32.Parse(pagesLocation[j.ToString()].Substring(6, 1));
                    cylinderNumber -= 4;
                    cylinderText = pagesLocation[j.ToString()].Substring(3, 3) + cylinderNumber.ToString();
                    currentNode["cylinderName"].InnerText = cylinderText;
                    currentNode["cylinderSector"].InnerText = "Bottom";
                    currentNode["cylinderSectorName"].InnerText = "BOTTOM";

                    //YELLOW
                    currentNode = currentNode.NextSibling;
                    currentNode["towerName"].InnerText = pagesLocation[j.ToString()].Substring(0, 3);
                    currentNode["locationOnCylinder"].InnerText = pagesLocation[j.ToString()].Substring(8, 1);
                    cylinderNumber = Int32.Parse(pagesLocation[j.ToString()].Substring(6, 1));
                    cylinderNumber -= 2;
                    cylinderText = pagesLocation[j.ToString()].Substring(3, 3) + cylinderNumber.ToString();
                    currentNode["cylinderName"].InnerText = cylinderText;
                    currentNode["cylinderSector"].InnerText = "Top";
                    currentNode["cylinderSectorName"].InnerText = "TOP";
                    currentNode = currentNode.NextSibling;
                    currentNode["towerName"].InnerText = pagesLocation[j.ToString()].Substring(0, 3);
                    currentNode["locationOnCylinder"].InnerText = pagesLocation[j.ToString()].Substring(8, 1);
                    cylinderNumber = Int32.Parse(pagesLocation[j.ToString()].Substring(6, 1));
                    cylinderNumber -= 2;
                    cylinderText = pagesLocation[j.ToString()].Substring(3, 3) + cylinderNumber.ToString();
                    currentNode["cylinderName"].InnerText = cylinderText;
                    currentNode["cylinderSector"].InnerText = "Bottom";
                    currentNode["cylinderSectorName"].InnerText = "BOTTOM";
                    currentNode = currentNode.NextSibling;
                }

                publicationXML.Save(Path.Combine(outPath, template.selectedPublication));

                template.failureOrSuccess = "Dubbelprodukt skickad till Arkitex.";
            }
            else if (template.pagesAndLocation.Count == template.numberOfPages)
            {
                for (int j = 1; j < template.pagesAndLocation.Count + 1; j++)
                {
                    //BLACK
                    currentNode["towerName"].InnerText = pagesLocation[j.ToString()].Substring(0, 3);
                    currentNode["locationOnCylinder"].InnerText = pagesLocation[j.ToString()].Substring(8, 1);
                    currentNode["cylinderName"].InnerText = pagesLocation[j.ToString()].Substring(3, 4);

                    topOrBottom = pagesLocation[j.ToString()].Substring(8, 1);
                    if (Equals(topOrBottom, "H"))
                        topOrBottom = "Top";
                    else
                        topOrBottom = "Bottom";

                    currentNode["cylinderSector"].InnerText = topOrBottom;
                    currentNode["cylinderSectorName"].InnerText = topOrBottom.ToUpper();

                    currentNode = currentNode.NextSibling;

                    //CYAN
                    currentNode["towerName"].InnerText = pagesLocation[j.ToString()].Substring(0, 3);
                    currentNode["locationOnCylinder"].InnerText = pagesLocation[j.ToString()].Substring(8, 1);
                    cylinderNumber = Int32.Parse(pagesLocation[j.ToString()].Substring(6, 1));
                    cylinderNumber -= 6;
                    cylinderText = pagesLocation[j.ToString()].Substring(3, 3) + cylinderNumber.ToString();
                    currentNode["cylinderName"].InnerText = cylinderText;

                    topOrBottom = pagesLocation[j.ToString()].Substring(8, 1);
                    if (Equals(topOrBottom, "H"))
                        topOrBottom = "Top";
                    else
                        topOrBottom = "Bottom";

                    currentNode["cylinderSector"].InnerText = topOrBottom;
                    currentNode["cylinderSectorName"].InnerText = topOrBottom.ToUpper();


                    currentNode = currentNode.NextSibling;

                    //MAGENTA
                    currentNode["towerName"].InnerText = pagesLocation[j.ToString()].Substring(0, 3);
                    currentNode["locationOnCylinder"].InnerText = pagesLocation[j.ToString()].Substring(8, 1);
                    cylinderNumber = Int32.Parse(pagesLocation[j.ToString()].Substring(6, 1));
                    cylinderNumber -= 4;
                    cylinderText = pagesLocation[j.ToString()].Substring(3, 3) + cylinderNumber.ToString();
                    currentNode["cylinderName"].InnerText = cylinderText;

                    topOrBottom = pagesLocation[j.ToString()].Substring(8, 1);
                    if (Equals(topOrBottom, "H"))
                        topOrBottom = "Top";
                    else
                        topOrBottom = "Bottom";

                    currentNode["cylinderSector"].InnerText = topOrBottom;
                    currentNode["cylinderSectorName"].InnerText = topOrBottom.ToUpper();


                    currentNode = currentNode.NextSibling;

                    //YELLOW
                    currentNode["towerName"].InnerText = pagesLocation[j.ToString()].Substring(0, 3);
                    currentNode["locationOnCylinder"].InnerText = pagesLocation[j.ToString()].Substring(8, 1);
                    cylinderNumber = Int32.Parse(pagesLocation[j.ToString()].Substring(6, 1));
                    cylinderNumber -= 2;
                    cylinderText = pagesLocation[j.ToString()].Substring(3, 3) + cylinderNumber.ToString();
                    currentNode["cylinderName"].InnerText = cylinderText;

                    topOrBottom = pagesLocation[j.ToString()].Substring(8, 1);
                    if (Equals(topOrBottom, "H"))
                        topOrBottom = "Top";
                    else
                        topOrBottom = "Bottom";

                    currentNode["cylinderSector"].InnerText = topOrBottom;
                    currentNode["cylinderSectorName"].InnerText = topOrBottom.ToUpper();


                    currentNode = currentNode.NextSibling;

                }
                /*  
                 *  T11Cyl7LA
                 *  <towerName>First Tower</towerName>
                    <locationOnCylinder>A</locationOnCylinder>
                    <cylinderName>C7_black</cylinderName>
                    <cylinderSector>Top</cylinderSector>
                    <cylinderSectorName>TOP</cylinderSectorName>
                    */

                publicationXML.Save(Path.Combine(outPath, template.selectedPublication));

                template.failureOrSuccess = "Enkel produkt skickad till Arkitex.";
            }
            else
            {
                template.failureOrSuccess = "Sidantalet i templates stämmer inte överrens med sidantalet i den valda produkten";
            }

            //Send the XML to the prepress-system and set the view to reflect the success or failure of this operation.
            return View(template);
        }
    }
}
