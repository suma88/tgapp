using MyAuditApplication.BusinessLayer;
using MyAuditApplication.Models;
using MyAuditApplication.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyAuditApplication.Controllers
{
    public class ObjectController : Controller
    {
        // GET: FileUpload
        public ActionResult Upload()
        {          
            return View();
        }
        [HttpPost]
        public ActionResult ProcessCSV()
        {
            if (Request.Files.Count == 1)
            {
                var postedFile = Request.Files[0];
                if (postedFile.ContentLength > 0)
                {
                    List<Object> objects = new List<Object>();
                    using (var csvReader = new System.IO.StreamReader(postedFile.InputStream))
                    {
                        try
                        {
                            string inputLine = "";
                            csvReader.ReadLine();
                            while ((inputLine = csvReader.ReadLine()) != null)
                            {
                                string[] inputValues = inputLine.Split(new char[] { ',' });
                                Object obj = new Object()
                                {
                                    Id = inputValues[0],
                                    Type = inputValues[1],
                                    Timestamp = Convert.ToDouble(inputValues[2]),
                                    Changes = inputLine.Substring(inputLine.IndexOf(inputValues[3]))
                                };
                                objects.Add(obj);
                            }
                            csvReader.Close();
                        }
                        catch(Exception ex)
                        {
                            ViewBag.ErrorClass = Constants.CssErrorClass;
                            ViewBag.ErrorMessage = "Error: " + ex.Message;
                            return View("Upload", "_Layout", ViewBag.ErrorMessage);
                        }
                    }

                    ObjectBL.SaveObjects(objects);

                }
            }
            return Redirect("../");
        }

        //Get Search 
        public ActionResult Search()
        {
            return View();
        }

        [HttpPost]
        public string RetrieveObjectState(FormCollection form)
        {
            ObjectState objectState = null;
            try
            {
                 objectState = ObjectBL.FindObjectState(form["objectType"].ToString(), form["objectId"].ToString(), Convert.ToDouble(form["objectTimestamp"].ToString()));
                if (objectState.Code == Entities.ResultCode.NoRecordFound)
                    objectState.state = "#Object didn't exist at that time";
            }
            catch(Exception ex)
            {
                ViewBag.ErrorClass = Constants.CssErrorClass;
                ViewBag.ErrorMessage = "Error: " + ex.Message; 
                return View("Search", "_Layout", ViewBag.ErrorMessage);
            }
            ViewBag.ObjectState = objectState.state.ToString();
            return objectState.state.ToString(); 
        }
    }
}