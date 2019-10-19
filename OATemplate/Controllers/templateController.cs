using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OATemplate.Models;

namespace OATemplate.Controllers
{
    public class TemplateController : Controller
    {
        
        public ViewResult Index()
        {
            //Inital view just used to select a publication.
            return View();
        }
        
        [HttpPost]
        public ViewResult PublicationSelected(FormCollection values)
        {
            Template template = new Template();
            template.Publication = values["Publication"];
            //View to select a template or make a new one.
            return View(template);
        }

        [HttpPost]
        public string TemplateSelected()
        {
            //Fills in a form with the pages numbers from the selected saved templated.
            return "It was done.";
        }

        [HttpPost]
        public ViewResult TemplatedCommited()
        {
            //Sends the template to the prepress-system and sets the view to reflect the success or failure of this operation.
            return View();
        }
    }
}
