﻿using System;
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
            template.donePublications = new Dictionary<int, string>();

            //The program works with a few paths which are stored in Web.config.
            var appSettings = ConfigurationManager.AppSettings;
            string inPath = appSettings["In"];
            string donePath = appSettings["Done"];

            /* Publications are sorted in two different folders, incoming and those that have already been done.
             * Those that have already been done might have to be changed sometimes. */
            template.publications = GetPublication(inPath);
            template.donePublications = GetPublication(donePath);

            //Inital view just used to select a publication.
            return View(template);
        }

        [HttpPost]
        public ViewResult PublicationSelected(FormCollection values)
        {
            Template template = new Template();
            var appSettings = ConfigurationManager.AppSettings;
            string inPath = appSettings["In"];
            string donePath = appSettings["Done"];
            string savedTemplates = appSettings["Templates"];

            //The publication that was selected by the user is read into the model.
            template.selectedPublication = values["Publication"];
            template.selectedPublicationPath = values["selctPubPath"];

            //Read the associated XML file of the publication and figure out how many broadsheet pages there are in it.
            string xmlLocation;
            if (template.selectedPublicationPath == "Done")
                xmlLocation = Path.Combine(donePath, template.selectedPublication);
            else
                xmlLocation = Path.Combine(inPath, template.selectedPublication);
            XmlDocument publicationXML = new XmlDocument();
            publicationXML.Load(xmlLocation);
            XmlNode pagesNode = publicationXML.SelectSingleNode("Planning/productionRuns/productionRun/plates");
            XmlNodeList pageList = pagesNode.ChildNodes;
            template.numberOfPages = pageList.Count / 4;

            /* The templates are saved in subfolders based on the number of broadsheet pages in them. 
             * The directory for the templates with the correct number of broadsheet pages is the input to GetSavedTemplates.*/
            template.savedTemplates = GetSavedTemplates(Path.Combine(savedTemplates, pageList.Count.ToString()));

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
            string savedTemplates = appSettings["Templates"];
            template.selectedPublicationPath = values["selctPubPath"];


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

            /* The saved templates are loaded again so they can be displayed in the TemplateSelected view 
             * so the user doesn't have to restart everytime they want to select a new template. */
            template.savedTemplates = GetSavedTemplates(Path.Combine(savedTemplates, (template.numberOfPages * 4).ToString()));

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
            string donePath = appSettings["Done"];
            template.selectedPublication = values["selctPub"];
            int tempResult = Int32.Parse(values["NrPages"]);
            template.numberOfPages = tempResult;
            template.selectedTemplate = values["selctTemp"];
            template.selectedPublicationPath = values["selctPubPath"];

            //Moves the pages and their location into an dictionary.
            int i = 0;
            Dictionary<string, string> pagesLocation = new Dictionary<string, string>();
            while (i < values.Count && i < template.numberOfPages * 4)
            {
                string tempKey = values.GetKey(i);
                string tempValue = values.Get(i);
                if (tempValue.Length > 0 && !tempKey.Contains("selct") && !tempKey.Contains("NrPages"))
                    if (!pagesLocation.ContainsKey(tempValue))
                        pagesLocation.Add(tempValue, tempKey);

                i++;
            }
            template.pagesAndLocation = pagesLocation;

            /* Load the XML for the selected publication into a DOM. 
             * The pages  in <pages> will be looped through in ascending order and filled with the information from pagesLocation.*/
            string xmlLocation;
            if (template.selectedPublicationPath == "Done")
                xmlLocation = Path.Combine(donePath, template.selectedPublication);
            else
                xmlLocation = Path.Combine(inPath, template.selectedPublication);


            XmlDocument publicationXML = new XmlDocument();
            publicationXML.Load(xmlLocation);
            XmlNode currentNode = publicationXML.SelectSingleNode("Planning/productionRuns/productionRun/plates");
            currentNode = currentNode.FirstChild;

            /* If the production is Straight. */
            if (template.pagesAndLocation.Count * 2 == template.numberOfPages)
            {
                for (int j = 1; j < template.pagesAndLocation.Count + 1; j++)
                {
                    //CYAN
                    publicationXML = AddPage(publicationXML, currentNode, pagesLocation[j.ToString()], "Cyan");
                    currentNode = currentNode.NextSibling;
                    publicationXML = AddPage(publicationXML, currentNode, pagesLocation[j.ToString()], "Cyan");
                    currentNode = currentNode.NextSibling;

                    //MAGENTA
                    publicationXML = AddPage(publicationXML, currentNode, pagesLocation[j.ToString()], "Magenta");
                    currentNode = currentNode.NextSibling;
                    publicationXML = AddPage(publicationXML, currentNode, pagesLocation[j.ToString()], "Magenta");
                    currentNode = currentNode.NextSibling;

                    //YELLOW 
                    publicationXML = AddPage(publicationXML, currentNode, pagesLocation[j.ToString()], "Yellow");
                    currentNode = currentNode.NextSibling;
                    publicationXML = AddPage(publicationXML, currentNode, pagesLocation[j.ToString()], "Yellow");
                    currentNode = currentNode.NextSibling;

                    //BLACK
                    publicationXML = AddPage(publicationXML, currentNode, pagesLocation[j.ToString()], "Black");
                    currentNode = currentNode.NextSibling;
                    publicationXML = AddPage(publicationXML, currentNode, pagesLocation[j.ToString()], "Black");
                    currentNode = currentNode.NextSibling;

                }

                StreamWriter sw = new StreamWriter(Path.Combine(outPath, template.selectedPublication));
                sw.NewLine = "\n";
                publicationXML.Save(sw);
                sw.Close();

                template.failureOrSuccess = "Produkten skickad till Arkitex.";
            }
            /*If the productions is Collect*/
            else if (template.pagesAndLocation.Count == template.numberOfPages)
            {
                for (int j = 1; j < template.pagesAndLocation.Count + 1; j++)
                {
                    //CYAN
                    publicationXML = AddPage(publicationXML, currentNode, pagesLocation[j.ToString()], "Cyan");
                    currentNode = currentNode.NextSibling;

                    //MAGENTA
                    publicationXML = AddPage(publicationXML, currentNode, pagesLocation[j.ToString()], "Magenta");
                    currentNode = currentNode.NextSibling;

                    //YELLOW
                    publicationXML = AddPage(publicationXML, currentNode, pagesLocation[j.ToString()], "Yellow");
                    currentNode = currentNode.NextSibling;

                    //BLACK
                    publicationXML = AddPage(publicationXML, currentNode, pagesLocation[j.ToString()], "Black");
                    currentNode = currentNode.NextSibling;

                }

                StreamWriter sw = new StreamWriter(Path.Combine(outPath, template.selectedPublication));
                sw.NewLine = "\n";
                publicationXML.Save(sw);
                sw.Close();

                template.failureOrSuccess = "Produkten skickad till Arkitex.";
            }
            else
            {
                template.failureOrSuccess = "Sidantalet i templates stämmer inte överrens med sidantalet i den valda produkten";
            }

            //Publications that have been sent to Arkitex and are in the current in folder are moved to the "Done" folder to mark them as such.
            string sourceFile = Path.Combine(inPath, template.selectedPublication);
            string doneFile = Path.Combine(donePath, template.selectedPublication);
            if (template.selectedPublicationPath == "Current")
            {
                try
                {
                    System.IO.File.Move(sourceFile, doneFile);
                }
                catch (IOException)
                {
                    template.failureOrSuccess = "Publication was moved to Arkitex but couldn't be moved to done.";
                }
            }


            //Send the XML to the prepress-system and set the view to reflect the success or failure of this operation.
            return View(template);
        }


        /// <summary>
        /// Gets the colour which is specified in the name tag of the Cortex Planning interface.
        /// </summary>
        /// <param name="inNode">The node to get the colour of.</param>
        /// <returns>The colour of the node or an error if no colour was found. </returns>
        private string GetColour(XmlNode inNode)
        {

            string nameValue = inNode["name"].InnerText;

            string returnColour;

            if (nameValue.Contains("cyan"))
                returnColour = "Cyan";
            else if (nameValue.Contains("magenta"))
                returnColour = "Magenta";
            else if (nameValue.Contains("yellow"))
                returnColour = "Yellow";
            else if (nameValue.Contains("black"))
                returnColour = "Black";
            else
                returnColour = @"ERROR - No colour was found in <name>";

                return returnColour;
        }

        /// <summary>
        /// Adds a page to a Coretex Planning Interface XML file.
        /// </summary>
        /// <param name="inXML">A Coretex Planning Interface file in DOM form.</param>
        /// <param name="inNode">A page to add to the XML</param>
        /// <param name="pageLocation">A string with the information of where in the press the page should be.</param>
        /// <param name="colour">The colour of the page</param>
        /// <returns>An Coretex PLanning Interface XML.</returns>
        private XmlDocument AddPage(XmlDocument inXML, XmlNode inNode, string pageLocation, string colour)
        {
            int cylinderNumber;
            string cylinderText;
            string topOrBottom;

            inNode["towerName"].InnerText = pageLocation.Substring(0, 3);

            //This node does not exist in some versions of Arkitex and so is created if it doesn't.
            if (inNode["locationOnCylinder"] == null)
            {
                XmlNode newElem = inXML.CreateNode("element", "locationOnCylinder", "");
                newElem.InnerText = pageLocation.Substring(8, 1);
                inNode.AppendChild(newElem);
            }
            else
            {
                inNode["locationOnCylinder"].InnerText = pageLocation.Substring(8, 1);
            }

            //Cylinder number is calculated from the known cylinder in pageLocation and the colour.
            cylinderNumber = Int32.Parse(pageLocation.Substring(6, 1));
            if (string.Compare(colour, "Cyan") == 0)
                cylinderNumber -= 6;
            else if (string.Compare(colour, "Magenta") == 0)
                cylinderNumber -= 4;
            else if (string.Compare(colour, "Yellow") == 0)
                cylinderNumber -= 2;
            cylinderText = pageLocation.Substring(3, 3) + cylinderNumber.ToString() + "_" + colour;
            inNode["cylinderName"].InnerText = cylinderText;

            //Arkitex only uses top or bottom.
            topOrBottom = pageLocation.Substring(8, 1);
            if (Equals(topOrBottom, "H"))
                topOrBottom = "Top";
            else
                topOrBottom = "Bottom";

            inNode["cylinderSector"].InnerText = topOrBottom;
            inNode["cylinderSectorName"].InnerText = topOrBottom.ToUpper();

            return inXML;
        }

        /// <summary>
        /// Returns the name of all the templates/xml files in the inputed directory.
        /// </summary>
        /// <param name="pathToTemplates">The folder to get the xml from.</param>
        /// <returns>A string array of XML file names.</returns>
        private String[] GetSavedTemplates(string pathToTemplates)
        {
            string[] foundTemplates;

            //Template files are saved in subdirectories of savedTemplates based on their plate count.
            string[] allFilesInDir = Directory.GetFiles(pathToTemplates, @"*.xml");

            //Get the file names and put them into the model.
            foundTemplates = new string[allFilesInDir.Length];
            for (int i = 0; i < allFilesInDir.Length; i++)
            {
                foundTemplates[i] = Path.GetFileName(allFilesInDir[i]);
            }

            return foundTemplates;
        }

        /// <summary>
        /// Gets publications(XML files) from a folder.
        /// </summary>
        /// <param name="inPath">The path to search publications for.</param>
        /// <returns>A hastable where the elements are the found publications.</returns>
        private Dictionary<int, string> GetPublication(string inPath)
        {
            Dictionary<int, string> returnDictionary = new Dictionary<int, string>();

            //Get the different publications from their associated XML file from a folder.
            string[] allFilesInDir = Directory.GetFiles(inPath, @"*.XML");
            int i = 0;
            while (i < allFilesInDir.Length)
            {
                returnDictionary.Add(i, Path.GetFileName(allFilesInDir[i]));
                i++;
            }

            return returnDictionary;
        }
    }
}