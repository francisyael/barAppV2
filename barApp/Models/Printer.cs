using Spire.Pdf;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Zen.Barcode;

namespace barApp.Models
{
    public class Printer
    {
        private List<PrintPageEventHandler> Steps;

        private PrintDocument Document;
        private Font TitleFont;
        private Font SubtitleFont;
        private Font BodyFont;
        private Font BodyFontBold;
        private Brush Brush;
        private float Width;
        private float Height;
        private int Padding;
        private int MaxStringLines;
        private int ySeparation;
        private float yCurrent;

        public readonly KeyValuePair<string, string> EmptyListElement = new KeyValuePair<string, string>(string.Empty, string.Empty);

        public Printer(string printerRoute = "")
        {
            Document = new PrintDocument();
            Document.PrinterSettings.PrinterName = string.IsNullOrWhiteSpace(printerRoute) ? Document.PrinterSettings.PrinterName : printerRoute;
            TitleFont = new Font("Calibri", 14, FontStyle.Bold);
            SubtitleFont = new Font("Calibri", 8, FontStyle.Bold);
            BodyFont = new Font("Calibri", 8);
            BodyFontBold = new Font("Calibri", 8, FontStyle.Bold);
            Brush = new SolidBrush(Color.Black);
            Width = Document.DefaultPageSettings.PrintableArea.Width;
            Height = Document.DefaultPageSettings.PrintableArea.Height;
            Padding = 0;
            MaxStringLines = 5;
            ySeparation = 10;
            yCurrent = Padding;
            Steps = new List<PrintPageEventHandler>();
        }

        private bool MoveCursorDown(float value, PrintPageEventArgs _event)
        {
            bool needMorePages = (yCurrent + value) > (Height - Padding - TitleFont.Size);

            yCurrent = needMorePages ? Padding : (yCurrent + value);
            return _event.HasMorePages = needMorePages;
        }

        private float[] GetMap(float[] map)
        {
            if (map == null || map.Length == 0) return null;

            return map.Select(m => (Width - (Padding * 2)) * m / 100).ToArray();
        }

        public void AddTitle(string title)
        {
            PrintPageEventHandler function = null;
            function = new PrintPageEventHandler(delegate (object sender, PrintPageEventArgs _event)
            {
                if (Steps.Any(step => step == function) || _event.HasMorePages) return;

                _event.Graphics.DrawString(title.ToUpper(), TitleFont, Brush, Width / 2, yCurrent, new StringFormat() { Alignment = StringAlignment.Center });
                if (MoveCursorDown(_event.Graphics.MeasureString(title.ToUpper(), TitleFont).Height, _event)) return;

                Steps.Add(function);
            });

            Document.PrintPage += function;
        }

        public void AddSubtitle(string title)
        {
            AddLine();
            AddSpace(0.5f);

            PrintPageEventHandler function = null;
            function = new PrintPageEventHandler(delegate (object sender, PrintPageEventArgs _event)
            {
                if (Steps.Any(step => step == function) || _event.HasMorePages) return;

                StringFormat stringFormat = new StringFormat() { Alignment = StringAlignment.Center, Trimming = StringTrimming.Word };
                RectangleF baseLayout = new RectangleF(Padding, yCurrent, Width - (Padding * 2), Height);
                _event.Graphics.DrawString(title.ToUpper(), SubtitleFont, Brush, baseLayout, stringFormat);

                if (MoveCursorDown(_event.Graphics.MeasureString(title.ToUpper(), SubtitleFont, baseLayout.Size).Height, _event)) return;

                Steps.Add(function);
            });

            Document.PrintPage += function;
        }

        public void AddString(string text, bool bold = false, StringAlignment alignment = StringAlignment.Near)
        {
            PrintPageEventHandler function = null;
            function = new PrintPageEventHandler(delegate (object sender, PrintPageEventArgs _event)
            {
                if (Steps.Any(step => step == function) || _event.HasMorePages) return;

                StringFormat stringFormat = new StringFormat() { Alignment = alignment, Trimming = StringTrimming.Word };
                RectangleF baseLayout = new RectangleF(Padding, yCurrent, Width - (Padding * 2), Height);
                _event.Graphics.DrawString(text, (bold ? BodyFontBold : BodyFont), Brush, baseLayout, stringFormat);

                if (MoveCursorDown(_event.Graphics.MeasureString(text, (bold ? BodyFontBold : BodyFont)).Height, _event)) return;

                Steps.Add(function);
            });

            Document.PrintPage += function;
        }

        public void AddTable(string[] columns, string[][] data, bool hasHeader = false, float[] map = null)
        {
            int rowIndex = 0;

            AddLine();

            PrintPageEventHandler function = null;
            function = new PrintPageEventHandler(delegate (object sender, PrintPageEventArgs _event)
            {
                if (Steps.Any(step => step == function) || _event.HasMorePages) return;

                float[] _map = GetMap(map) ?? Enumerable.Repeat((Width - (Padding * (columns.Length + 1))) / columns.Length, columns.Length).ToArray();
                StringFormat stringFormat = new StringFormat { Trimming = StringTrimming.Word };
                float biggestColumnHeight = 0;
                float xCurrent = Padding;

                for (int index = 0; index < columns.Length; index++)
                {
                    float baseHeight = _event.Graphics.MeasureString(columns[index], SubtitleFont).Height;
                    RectangleF baseLayout = new RectangleF(xCurrent, yCurrent, _map[index], baseHeight * MaxStringLines);
                    _event.Graphics.DrawString(columns[index], SubtitleFont, Brush, baseLayout, stringFormat);

                    float currentHeight = _event.Graphics.MeasureString(columns[index], BodyFont, baseLayout.Size).Height;
                    biggestColumnHeight = currentHeight > biggestColumnHeight ? currentHeight : biggestColumnHeight;

                    xCurrent += baseLayout.Width + Padding;
                }

                if (MoveCursorDown(biggestColumnHeight, _event)) return;
                _event.Graphics.DrawLine(new Pen(Brush), Padding / 2, yCurrent, Width - (Padding / 2), yCurrent);
                if (MoveCursorDown(15, _event)) return;

                for (; rowIndex < data.Length; rowIndex++)
                {
                    if (hasHeader)
                    {
                        float baseHeight = _event.Graphics.MeasureString(data[rowIndex][0], BodyFont).Height;
                        RectangleF baseLayout = new RectangleF(Padding, yCurrent, Width - Padding, baseHeight * MaxStringLines);
                        _event.Graphics.DrawString(data[rowIndex][0], BodyFont, Brush, baseLayout, stringFormat);
                        if (MoveCursorDown(_event.Graphics.MeasureString(data[rowIndex][0], BodyFont, baseLayout.Size).Height - 5, _event)) return;
                    }

                    biggestColumnHeight = 0;
                    xCurrent = Padding;

                    for (int columnIndex = (hasHeader ? 1 : 0); columnIndex < data[rowIndex].Length; columnIndex++)
                    {
                        float baseHeight = _event.Graphics.MeasureString(data[rowIndex][columnIndex], BodyFont).Height;
                        RectangleF baseLayout = new RectangleF(xCurrent, yCurrent, _map[columnIndex - (hasHeader ? 1 : 0)], baseHeight * MaxStringLines);
                        _event.Graphics.DrawString(data[rowIndex][columnIndex], BodyFont, Brush, baseLayout, stringFormat);

                        float currentHeight = _event.Graphics.MeasureString(data[rowIndex][columnIndex], BodyFont, baseLayout.Size).Height;
                        biggestColumnHeight = currentHeight > biggestColumnHeight ? currentHeight : biggestColumnHeight;

                        xCurrent += baseLayout.Width + Padding;
                    }

                    if (MoveCursorDown(biggestColumnHeight + 10, _event)) return;
                }

                Steps.Add(function);
            });

            Document.PrintPage += function;

            AddLine();
        }

        public void AddTableDetails(IDictionary<string, string> data, int tableColumns)
        {
            int index = 0;

            PrintPageEventHandler function = null;
            function = new PrintPageEventHandler(delegate (object sender, PrintPageEventArgs _event)
            {
                if (Steps.Any(step => step == function) || _event.HasMorePages) return;

                StringFormat stringFormat = new StringFormat() { Trimming = StringTrimming.Word };
                float xGridSize = (Width - (Padding * 2)) / tableColumns;

                for (; index < data.Count; index++)
                {
                    _event.Graphics.DrawString(data.ElementAt(index).Key + ":", BodyFontBold, Brush, (Padding * 2 + (xGridSize * (tableColumns - 2))), yCurrent, stringFormat);
                    _event.Graphics.DrawString(data.ElementAt(index).Value, BodyFont, Brush, (Padding * 2 + (xGridSize * (tableColumns - 1))), yCurrent, stringFormat);

                    if (MoveCursorDown(_event.Graphics.MeasureString(data.ElementAt(index).Key, BodyFontBold).Height, _event)) return;
                }

                Steps.Add(function);
            });

            Document.PrintPage += function;
        }

        public void AddDescriptionList(IDictionary<string, string> data, int columns = 1)
        {
            int index = 0;
            float xGridSize = (Width - (Padding * 2)) / columns;

            PrintPageEventHandler function = null;
            function = new PrintPageEventHandler(delegate (object sender, PrintPageEventArgs _event)
            {
                if (Steps.Any(step => step == function) || _event.HasMorePages) return;

                for (; index < data.Count; index++)
                {
                    float yBase = 1;

                    if (columns == 2) yBase = index % columns;
                    if (columns == 3)
                    {
                        int divisor = columns;
                        int modulus = index % divisor;
                        int fase = (index + 1) % divisor;

                        yBase = (modulus - fase) / (columns - 1);
                    }

                    float xBase = Padding + (xGridSize * (index % columns));
                    SizeF keySize = _event.Graphics.MeasureString(data.ElementAt(index).Key + ": ", BodyFontBold);

                    if (!string.IsNullOrEmpty(data.ElementAt(index).Key))
                    {
                        _event.Graphics.DrawString(data.ElementAt(index).Key + ": ", BodyFontBold, Brush, xBase, yCurrent);
                        _event.Graphics.DrawString(data.ElementAt(index).Value, BodyFont, Brush, (xBase + keySize.Width), yCurrent);
                    }

                    if (MoveCursorDown(yBase * keySize.Height, _event)) return;
                }

                Steps.Add(function);
            });

            Document.PrintPage += function;
        }

        public void AddBarCode(string text)
        {
            PrintPageEventHandler function = null;
            function = new PrintPageEventHandler(delegate (object sender, PrintPageEventArgs _event)
            {
                if (Steps.Any(step => step == function) || _event.HasMorePages) return;

                Image barcode = BarcodeDrawFactory.GetSymbology(BarcodeSymbology.Code128).Draw(text, 36);
                _event.Graphics.DrawImage(barcode, (Width - barcode.Width) / 2, yCurrent);

                if (MoveCursorDown(barcode.Height, _event)) return;

                Steps.Add(function);
            });

            Document.PrintPage += function;

            AddString(text, false, StringAlignment.Center);
        }

        public void AddLine()
        {
            PrintPageEventHandler function = null;
            function = new PrintPageEventHandler(delegate (object sender, PrintPageEventArgs _event)
            {
                if (Steps.Any(step => step == function) || _event.HasMorePages) return;

                _event.Graphics.DrawLine(new Pen(Brush), Padding / 2, yCurrent, Width - (Padding / 2), yCurrent);

                Steps.Add(function);
            });

            Document.PrintPage += function;
        }

        public void AddSpace(float modifier = 1)
        {
            PrintPageEventHandler function = null;
            function = new PrintPageEventHandler(delegate (object sender, PrintPageEventArgs _event)
            {
                if (Steps.Any(step => step == function) || _event.HasMorePages) return;

                if (MoveCursorDown(ySeparation * modifier, _event)) return;

                Steps.Add(function);
            });

            Document.PrintPage += function;
        }

        public void Print(string Tipo= "",string HTMLCorreo ="")
        {

            string IP = System.Configuration.ConfigurationManager.AppSettings["Noip"].ToString();

            if (IP.ToUpper() == "NO")
            {

            string ruta = "";

            if (Tipo == "Cuadre")
            {
                //ruta = @"C:\Cuadres\" + System.DateTime.Now.ToString("ddMMyyyymmss") + ".pdf";
                //Document.PrinterSettings.PrintToFile = true;
                //Document.PrinterSettings.PrintFileName = ruta;

                //  string printerRoute = "";
                string printerRoute = "";
                Document.PrinterSettings.PrinterName = string.IsNullOrWhiteSpace(printerRoute) ? Document.PrinterSettings.PrinterName : printerRoute; // */

                Document.Print();

                    //PdfDocument doc = new PdfDocument();
                    //doc.LoadFromFile(ruta);
                    //doc.PrinterName = string.IsNullOrWhiteSpace(printerRoute) ? Document.PrinterSettings.PrinterName : printerRoute;// impresora;
                    //doc.PrintDocument.Print();


                    //Spire.Pdf.PdfDocument  PrintDocument pd = new Spire.Pdf.PdfDocument();
                    //pd.PrinterSettings.PrintFileName = ruta;         
                    //pd.PrinterSettings.PrinterName = string.IsNullOrWhiteSpace(printerRoute) ? Document.PrinterSettings.PrinterName : printerRoute;
                    //pd.prindo.pri.Print();

                    //PdfDocument pdfdocument = new PdfDocument();
                    //pdfdocument.LoadFromFile(pdfPathAndFileName);
                    //pdfdocument.PrinterName = "My Printer";
                    //pdfdocument.PrintDocument.PrinterSettings.Copies = 1;
                    //pdfdocument.PrintDocument.Print();
                    //pdfdocument.Dispose();

                    try
                    {


                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.google.com/");
                    request.Timeout = 5000;
                    request.Credentials = CredentialCache.DefaultNetworkCredentials;
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    using (var contex = new barbdEntities())
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var ListadoCorreo = contex.Usuario.Where(x => x.Correo != "" && x.EnvioCorreo == true).ToList();

                            foreach (var item in ListadoCorreo)
                            {
                                EnviarMail("multicoretech01@gmail.com", item.Correo, "Cuadre del dia", "Cuadre Correspondiente al " + System.DateTime.Now.ToLongDateString(), "multicoretech01@gmail.com", "Numero18", HTMLCorreo);
                            }

                        }

                    }


                }
                catch (System.Exception ex)
                {

                }



            }
            else
            {
                string Ip = "";

                using (var context = new barbdEntities())
                {


                    //var host = Dns.GetHostEntry(Dns.GetHostName());

                    //foreach (var ip in host.AddressList)
                    //{
                    //    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    //    {
                    //        Ip = ip.ToString();
                    //    }
                    //}

                    //var OjbImpresora = context.Impresoras.SingleOrDefault(x => x.IpPC == Ip);

                    string printerRoute = "";

                    Document.PrinterSettings.PrinterName = string.IsNullOrWhiteSpace(printerRoute) ? Document.PrinterSettings.PrinterName : printerRoute; // OjbImpresora.defecto == true ? string.IsNullOrWhiteSpace(printerRoute) ? Document.PrinterSettings.PrinterName : printerRoute : OjbImpresora.IpImpresora;
                    Document.Print();
                }
            }


            }
            else
            {

                string ruta = "";

                if (Tipo == "Cuadre")
                {
                    //ruta = @"C:\Cuadres\" + System.DateTime.Now.ToString("ddMMyyyymmss") + ".pdf";
                    //Document.PrinterSettings.PrintToFile = true;
                    //Document.PrinterSettings.PrintFileName = ruta;

                    //  string printerRoute = "";
                  //  string printerRoute = "";
                 //   Document.PrinterSettings.PrinterName = "Microsoft Print to PDF";
                    //Document.PrinterSettings.PrinterName = string.IsNullOrWhiteSpace(printerRoute) ? Document.PrinterSettings.PrinterName : printerRoute; // "Microsoft Print to PDF";

                    //Document.Print();

                    //PdfDocument doc = new PdfDocument();
                    //doc.LoadFromFile(ruta);
                    //doc.PrinterName = string.IsNullOrWhiteSpace(printerRoute) ? Document.PrinterSettings.PrinterName : printerRoute;// impresora;
                    //doc.PrintDocument.Print();

                    string Ip = "";


                    using (var context = new barbdEntities())
                    {

                        var host = Dns.GetHostEntry(Dns.GetHostName());

                        //foreach (var ip in host.AddressList)
                        //{
                        //    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        //    {
                        //        Ip = ip.ToString();
                        //    }
                        //}

                        Ip = System.Net.Dns.GetHostName();

                        var OjbImpresora = context.Impresoras.SingleOrDefault(x => x.IpPC == Ip);

                        string printerRoute = "";

                        Document.PrinterSettings.PrinterName = OjbImpresora.defecto == true ? string.IsNullOrWhiteSpace(printerRoute) ? Document.PrinterSettings.PrinterName : printerRoute : OjbImpresora.IpImpresora;
                        Document.Print();
                    }

                    try
                    {


                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.google.com/");
                        request.Timeout = 5000;
                        request.Credentials = CredentialCache.DefaultNetworkCredentials;
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                        using (var contex = new barbdEntities())
                        {
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                var ListadoCorreo = contex.Usuario.Where(x => x.Correo != "" && x.EnvioCorreo == true).ToList();

                                foreach (var item in ListadoCorreo)
                                {
                                    EnviarMail("multicoretech01@gmail.com", item.Correo, "Cuadre del dia", "Cuadre Correspondiente al " + System.DateTime.Now.ToLongDateString(), "multicoretech01@gmail.com", "Numero18", HTMLCorreo);
                                }

                            }

                        }


                    }
                    catch (System.Exception ex)
                    {

                    }



                    //pd.PrinterSettings.PrinterName = string.IsNullOrWhiteSpace(printerRoute) ? Document.PrinterSettings.PrinterName : printerRoute;
                    //pd.Print();


                    //PdfDocument pdfdocument = new PdfDocument();
                    //pdfdocument.LoadFromFile(pdfPathAndFileName);
                    //pdfdocument.PrinterName = "My Printer";
                    //pdfdocument.PrintDocument.PrinterSettings.Copies = 1;
                    //pdfdocument.PrintDocument.Print();
                    //pdfdocument.Dispose();

                    //try
                    //{


                    //    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.google.com/");
                    //    request.Timeout = 5000;
                    //    request.Credentials = CredentialCache.DefaultNetworkCredentials;
                    //    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    //    using (var contex = new barbdEntities())
                    //    {
                    //        if (response.StatusCode == HttpStatusCode.OK)
                    //        {
                    //            var ListadoCorreo = contex.Usuario.Where(x => x.Correo != "" && x.EnvioCorreo == true).ToList();

                    //            foreach (var item in ListadoCorreo)
                    //            {
                    //                EnviarMail("multicoretech01@gmail.com", item.Correo, "Cuadre del dia", "Cuadre Correspondiente al " + System.DateTime.Now.ToLongDateString(), "multicoretech01@gmail.com", "Numero18", ruta);
                    //            }

                    //        }

                    //    }


                    //}
                    //catch (System.Exception ex)
                    //{

                    //}



                }
                else
                {
                    string Ip = "";

                    using (var context = new barbdEntities())
                    {

                        //var host = Dns.GetHostEntry(Dns.GetHostName());

                        //foreach (var ip in host.AddressList)
                        //{
                        //    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        //    {
                        //        Ip = ip.ToString();
                        //    }
                        //}

                        Ip = System.Net.Dns.GetHostName();

                        var OjbImpresora = context.Impresoras.SingleOrDefault(x => x.IpPC == Ip);

                        string printerRoute = "";
               
                        Document.PrinterSettings.PrinterName = OjbImpresora.defecto == true ? string.IsNullOrWhiteSpace(printerRoute) ? Document.PrinterSettings.PrinterName : printerRoute : OjbImpresora.IpImpresora;
                        Document.Print();
                    }
                }

            }


        }

        private void EnviarMail(string de, string para, string asunto, string contenido, string usuario, string contraseña, string html)
        {
            //MailMessage _Correo = new MailMessage();


            //    _Correo.From = new MailAddress(de);

            //    _Correo.To.Add(para);
            //    _Correo.Subject = asunto;
            //    _Correo.Body = contenido;
            //    _Correo.IsBodyHtml = false; // Le indicamos que el cuerpo del mensaje no es HTLM
            //    _Correo.Priority = MailPriority.Normal;


            //    Attachment _attachment = new Attachment(rutaAdjunto);
            //    _Correo.Attachments.Add(_attachment);


            //        SmtpClient smtp = new SmtpClient();
            //        smtp.Credentials = new NetworkCredential(usuario, contraseña);

            //        smtp.Host = "smtp.gmail.com";
            //        smtp.Port = 465;
            //        smtp.EnableSsl = true;




          //  string filename = rutaAdjunto;
            //Attachment data = new Attachment(filename, MediaTypeNames.Application.Octet);

            SmtpClient client = new SmtpClient();
            client.Port = 587;
            // utilizamos el servidor SMTP de gmail
            client.Host = "smtp.gmail.com";
            client.EnableSsl = true;
            client.Timeout = 1000000;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            // nos autenticamos con nuestra cuenta de gmail
            client.Credentials = new NetworkCredential(de, "Numero18");

            MailMessage mail = new MailMessage("Multitepro@Multicore.com", para, "Cuadre", "Cuadre correspondiente al" + System.DateTime.Now.ToLongDateString());
            mail.BodyEncoding = UTF8Encoding.UTF8;
            mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
            mail.IsBodyHtml= true;
            mail.Body = html;
           // mail.Attachments.Add(data);
            client.Send(mail);


        }

        private void EnviarMail1(string de, string para, string asunto, string contenido, string usuario, string contraseña, string html)
        {
            //MailMessage _Correo = new MailMessage();


            //    _Correo.From = new MailAddress(de);

            //    _Correo.To.Add(para);
            //    _Correo.Subject = asunto;
            //    _Correo.Body = contenido;
            //    _Correo.IsBodyHtml = false; // Le indicamos que el cuerpo del mensaje no es HTLM
            //    _Correo.Priority = MailPriority.Normal;


            //    Attachment _attachment = new Attachment(rutaAdjunto);
            //    _Correo.Attachments.Add(_attachment);


            //        SmtpClient smtp = new SmtpClient();
            //        smtp.Credentials = new NetworkCredential(usuario, contraseña);

            //        smtp.Host = "smtp.gmail.com";
            //        smtp.Port = 465;
            //        smtp.EnableSsl = true;




            //  string filename = rutaAdjunto;
            //Attachment data = new Attachment(filename, MediaTypeNames.Application.Octet);

            SmtpClient client = new SmtpClient();
            client.Port = 587;
            // utilizamos el servidor SMTP de gmail
            client.Host = "smtp.gmail.com";
            client.EnableSsl = true;
            client.Timeout = 1000000;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            // nos autenticamos con nuestra cuenta de gmail
            client.Credentials = new NetworkCredential(de, "Numero18");

            MailMessage mail = new MailMessage("Multitepro@Multicore.com", para, "Producto Eliminado", "Fecha:" + System.DateTime.Now.ToLongDateString());
            mail.BodyEncoding = UTF8Encoding.UTF8;
            mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
            mail.IsBodyHtml = true;
            mail.Body = html;
            // mail.Attachments.Add(data);
            client.Send(mail);


        }


        public void CorreoEliminado(string HTMLCorreo)

        {
            
            try
            {


                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.google.com/");
                request.Timeout = 5000;
                request.Credentials = CredentialCache.DefaultNetworkCredentials;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (var contex = new barbdEntities())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var ListadoCorreo = contex.Usuario.Where(x => x.Correo != "" && x.EnvioCorreo == true).ToList();

                        foreach (var item in ListadoCorreo)
                        {
                            EnviarMail1("multicoretech01@gmail.com", item.Correo, "Producto Elimminado", "Fecha:" + System.DateTime.Now.ToLongDateString(), "multicoretech01@gmail.com", "Numero18", HTMLCorreo);
                        }

                    }

                }


            }
            catch (System.Exception ex)
            {

            }

        }
    }
}