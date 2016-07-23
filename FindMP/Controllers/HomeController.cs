using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Controtex;

namespace FindMP.Controllers
{
    [SuppressMessage("ReSharper", "StringLastIndexOfIsCultureSpecific.1")]
    [SuppressMessage("ReSharper", "StringIndexOfIsCultureSpecific.2")]
    [SuppressMessage("ReSharper", "StringIndexOfIsCultureSpecific.1")]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult TC()
        {
            return View();
        }

        public ActionResult Confirmation(string id)
        {
            var uid = Guid.Parse(id);
            LetterData data = new LetterData(uid);
            SendEmailFromData(data);
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        public ActionResult FindMP(string postcode)
        {
            var path = $"http://data.parliament.uk/membersdataplatform/services/mnis/members/query/fymp={postcode}/Addresses/";
            var res = DownloadPage(path, reqcontent: "application/json");
            return Json(new { data = res });
        }

        public ActionResult sendEmail(LetterData data)
        {
            var ctx = new ImmigrantsDataContext();
            var row = new MPEMail {Confirmed = false, Body = data.Body, Subject = data.Subject, Name = data.Name, MP=data.MP, Email = data.Email, PostCode = data.PostCode, Country = data.Country, UniqueId = new Guid(), MPEmail1 = data.MPEmail};
            row.UniqueId = Guid.NewGuid();
            ctx.MPEMails.InsertOnSubmit(row);
            ctx.SubmitChanges();
            var to = data.Email;
            var url = Request.Url.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
            var link = $"<div><a href='{url}/Home/Confirmation/{row.UniqueId}'>Confirm that you wish to send this e-mail</a></div>";
            var separator = "<p>" + new string('-', 50) + "</p>";
            var smtp = new SmtpClient("localhost");
            MailMessage message = new MailMessage
            {
                Subject = $"Your request to send letter to your MP ({data.MP})",
                Body = "<p>This email has been generated because you attempted to send an email to your MP from appel.immigrants.help.</p>"+
                "<p>Before we can proceed with your request, we need you to confirm that you intended to send the email by clicking on the link below:</p>"+
                link+
                "<p>Thank you for voicing your concerns. It is important that we understand the views of our representatives on issues of importance to us.</p>"+
                "<p>Immigrants' Helpline</p>"
                /*separator+row.Body+ separator*/,
                From = new MailAddress("noreply@immigrants.help"),
                IsBodyHtml = true
            };
            message.To.Add(to);
            message.CC.Add("info@immigrants.help");
            

            smtp.Send(message);
            return Json( new { success = true});
        }

        private static void SendEmailFromData(LetterData data)
        {
            var to = data.MPEmail;
            to = "zbych+test@controtex.com";
            var smtp = new SmtpClient("localhost");
            MailMessage message = new MailMessage
            {
                Subject = data.Subject,
                Body = data.Body,
                From = new MailAddress(data.Email),
                IsBodyHtml = true
            };
            message.To.Add(to);
            message.CC.Add("info@immigrants.help");
            message.CC.Add(data.Email);

            smtp.Send(message);
        }

        protected static string DownloadPage(string url, bool usePost = false, List<string[]> query = null, string PageEncoding = "", string reqcontent= "application/x-www-form-urlencoded")
        {
            string strValue = ListToQueryString(query);
            if (!usePost && strValue.IsNotEmpty()) url += "?" + strValue;
            var req = (HttpWebRequest)WebRequest.Create(url);


            req.Referer = url.Substring(0, url.LastIndexOfAny(new[] { '/' }));
            // Set values for the request back 
            if (usePost)
            {
                req.Method = "POST";
                req.ContentType = reqcontent;
                req.ContentLength = strValue.Length;
                // Write the request 
                var stOut = new StreamWriter(req.GetRequestStream(), Encoding.ASCII);
                stOut.Write(strValue);
                stOut.Close();
            }

            if (reqcontent.IsNotEmpty()) req.Accept = reqcontent;

            using (var resp = (HttpWebResponse)req.GetResponse())
            {
                byte[] buffer;
                using (Stream s = resp.GetResponseStream())
                {
                    buffer = ReadStream(s);
                }

                string pageEncoding = "";
                Encoding e = Encoding.UTF8;
                if (url.Contains(".hu/")) pageEncoding = "cp1252";
                if (resp.ContentEncoding != "") pageEncoding = resp.ContentEncoding;
                else if (resp.CharacterSet != "") pageEncoding = resp.CharacterSet;
                else if (resp.ContentType != "") pageEncoding = GetCharacterSet(resp.ContentType);

                if (pageEncoding == "") pageEncoding = GetCharacterSet(buffer);

                if (pageEncoding != null && pageEncoding.IsNotEmpty())
                {
                    try
                    {
                        e = pageEncoding.StartsWith("cp")
                                ? Encoding.GetEncoding(pageEncoding.Substring(2).ToInt32())
                                : Encoding.GetEncoding(pageEncoding);
                    }
                    catch
                    {
                        //throw new Exception("Invalid encoding: " + pageEncoding);
                    }
                }

                string data = e.GetString(buffer);
                string header =
                    Regex.Match(data, "<head.*?<body", RegexOptions.Singleline | RegexOptions.IgnoreCase).Value;
                string content = Regex.Match(header, "charset=windows-[0-9]*").Value;
                if (content.IsNotEmpty())
                {
                    int codePage = Regex.Match(header, "charset=windows-([0-9]*)").Groups[1].Value.ToInt32();
                    data = Encoding.GetEncoding(codePage).GetString(buffer);
                }
                else
                {
                    content = Regex.Match(header, "charset=utf-8").Value;
                    if (content.IsNotEmpty()) data = Encoding.UTF8.GetString(buffer);
                }

                //                Status = "";

                return data;
            }

        }
        private static string GetCharacterSet(string s)
        {
            s = s.ToUpper();
            int start = s.LastIndexOf("CHARSET");
            if (start == -1) return "";

            start = s.IndexOf("=", start);
            if (start == -1) return "";

            start++;
            s = s.Substring(start).Trim();
            int end = s.Length;

            int i = s.IndexOf(";");
            if (i != -1) end = i;
            i = s.IndexOf("\"");
            if (i != -1 && i < end) end = i;
            i = s.IndexOf("'");
            if (i != -1 && i < end) end = i;
            i = s.IndexOf("/");
            if (i != -1 && i < end) end = i;

            return s.Substring(0, end).Trim();
        }

        private static string GetCharacterSet(byte[] data)
        {
            string s = Encoding.Default.GetString(data);
            return GetCharacterSet(s);
        }

        private static byte[] ReadStream(Stream s)
        {
            try
            {
                var buffer = new byte[8096];
                using (var ms = new MemoryStream())
                {
                    while (true)
                    {
                        int read = s.Read(buffer, 0, buffer.Length);
                        if (read <= 0) return ms.ToArray();
                        ms.Write(buffer, 0, read);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string ListToQueryString(IEnumerable<string[]> query)
        {
            string strValue = "";
            if (query != null)
            {
                strValue = query.Aggregate(strValue, (current, s) => current + String.Format("&{0}={1}", s[0], s[1]));
                strValue = strValue.Substring(1);
            }
            return strValue;
        }

    }

    public class LetterData
    {
        public LetterData()
        {
        }

        public LetterData(Guid uid)
        {
            var ctx = new ImmigrantsDataContext();
            var r = ctx.MPEMails.FirstOrDefault(d => d.UniqueId == uid);
            if(r==null) throw new Exception($"Email {uid} not found");
            Name = r.Name;
            Email = r.Email;
            PostCode = r.PostCode;
            Country = r.Country;
            MP = r.MP;
            Body = r.Body;
            Subject = r.Subject;
            Confirmed = r.Confirmed.GetValueOrDefault();
            MPEmail = r.MPEmail1;
            UniqueId = r.UniqueId;
        }

        public string Name { get; set; }
        public string Email { get; set; }
        public string PostCode { get; set; }
        public string Country { get; set; }
        public string MP { get; set; }
        public string Body { get; set; }
        public string Subject { get; set; }
        public bool Confirmed { get; set; }
        public string MPEmail { get; set; }
        public Guid? UniqueId { get; set; }
    }
}