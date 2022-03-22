using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace barApp.Models
{
    public class HtmlCorreo
    {

       public string HTML="";
        
        public string AddTitle(string Title)
        {

            string htmlTitle = $"<h2>{Title}</h2>";
            HTML += htmlTitle;
            return htmlTitle;

        }

        public string AddSpace(int Space)
        {
            string htmlSpace = "";

            for (int i = 0; i < Space; i++)
            {
                htmlSpace += "</br>";

            }
            HTML += htmlSpace+"<hr>";
            return htmlSpace;
        }

        public string AddSubtitle(string Title)
        {

            string htmlTitle = $"<h3>{Title}</h3>";
            HTML += htmlTitle;
            return htmlTitle;

        }

        public string AddDescriptionList(IDictionary<string, string> data, int columns = 1)
        {
            string Tabla = "";
            string htmlTalbe = "<table borde='2>";
            string htmlTalbeClose = "</table>";
            string tableTr = "<tr border='1'>";
            string tableTrClose = "</tr>";   

            int i = 0;

            foreach (var item in data)
            {


                if (i == 0)
                {
                     
                    Tabla += $"<th>{item.Key}:{item.Value}</th>"; ;
                }
                else
                {
               
                    Tabla += $"<th>{item.Key}:{item.Value}</th>";

                }

                i++;

            }
            Tabla = htmlTalbe + tableTr + Tabla + tableTrClose + htmlTalbeClose;
            

            HTML += Tabla;
            return Tabla;

        
        }

       
        public string AddTable(string[] columns, string[][] data, bool hasHeader = false, float[] map = null)
        {
            string Tabla = "";
            string htmlTalbe = "<table borde='2>";
            string htmlTalbeClose = "</table>";
            string tableTr = "<tr border='1'>";
            string tableTrClose = "</tr>";
            string tableTh = "";


            for(int i = 0; i < columns.Count(); i++)
            {

                tableTh +=  "<th>" + columns[i].ToString() + "</th>" ;
            }

            Tabla += tableTr + tableTh + tableTrClose;

            tableTh = "";

            int ii;
            foreach (var item in data)
            {
                for (ii=0; ii < columns.Count(); ii++)
                {
                    tableTh +=  "<td>" + item[ii].ToString() + "</td>" ;
                }
                ii= 0;
                Tabla += tableTr + tableTh + tableTrClose;
                tableTh = "";

            };

           

            Tabla = htmlTalbe + Tabla + htmlTalbeClose;

            HTML += Tabla;
            return Tabla;



        }

        public string AddTableDetails(IDictionary<string, string> data, int tableColumns)
        {
            string Tabla = "";
            string htmlTalbe = "<table borde='2>";
            string htmlTalbeClose = "</table>";
            string tableTr = "<tr border='1'>";
            string tableTrClose = "</tr>";
            string tableTh = "";


            foreach (var item in data)
            {
                for (int i = 0; i < tableColumns; i++)
                {
                    tableTh += "<th></th>";                  
                }
            
                tableTh += "<th>" + data.ElementAt(0).Key+ "</th>";
                
            }

            Tabla += htmlTalbe + tableTr + tableTh + tableTrClose + htmlTalbeClose;
            HTML += Tabla;
            return Tabla;

        }

        public string html()
        {

                                          


            string h = "<html><head><style>table {font-family: arial, sans-serif;  border-collapse: collapse;  width: 100%;}td, th {  border: 1px solid #dddddd;  text-align: left;  padding: 8px;}tr:nth-child(even) {  background - color: #dddddd;}</style></head><body>"+HTML+"</body></html>";
            return h;
        }

    }
}