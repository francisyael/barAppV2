using barApp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace barApp.Controllers
{//DD

    public class HomeController : Controller
    {
        private string itbis_ = System.Configuration.ConfigurationManager.AppSettings["itbis"].ToString();
        private string propina_ = System.Configuration.ConfigurationManager.AppSettings["propina"].ToString();
        private string TipoCategoria = System.Configuration.ConfigurationManager.AppSettings["TipoCategoria"].ToString();

        public HomeController()
        {
            ViewData["Alert"] = null;

        }

        #region Dashboard
        //inicio
        public ActionResult Index()
        {
            using (var entity = new barbdEntities())
            {

                if (Session["Usuario"] == null)
                {

                    return RedirectToAction("Index", "Login");

                }
                ViewData["TipoCategoria"] = TipoCategoria;
                ViewBag.VendedorOrigen = entity.Usuario.Where(x => x.idRol == 2 || x.idRol == 7 && x.activo == true).Select(x => new { x.idUsuario, x.nombre }).ToList();
                ViewData["ModoPago"] = entity.ModoPago.Where(m => m.numPago > 0).ToList();
                ViewData["ClienteOrigen"] = new List<Cliente>();

                int? cuadreActual = entity.Cuadre.AsEnumerable().SingleOrDefault(c => !c.cerrado.GetValueOrDefault(false))?.idCuadre;
                IEnumerable<Venta> cuentas = entity.Venta.Where(v => v.idCuadre == cuadreActual).AsEnumerable().Select(v => UntrackedVenta(v));
                ViewBag.Cuentas = cuentas.ToArray();
                ViewBag.Detalles = entity.DetalleVenta.AsEnumerable().Where(dv => cuentas.Select(c => c.idVenta).Contains(dv.idVenta)).Select(dv => UntrackedDetalle(dv)).ToArray();
                ViewBag.Vendedores = entity.Usuario.Where(u => u.idRol == 2 || u.idRol == 7 && u.activo == true).AsEnumerable().Select(u => UntrackedUsuario(u)).ToArray();
                ViewBag.Usuarios = entity.Usuario.Where(x => x.activo == true).AsEnumerable().Select(u => UntrackedUsuario(u)).ToArray();
                ViewBag.GastosTotal = JsonConvert.SerializeObject(entity.Gastos.Where(g => g.idCuadre == cuadreActual).Sum(g => g.cantidad));
                ViewBag.Efectivo = entity.Factura.Include("venta").Where(x => x.Venta.idCuadre == cuadreActual && x.numPago == 1 && x.TieneCedito == false).DefaultIfEmpty(null).Sum(x => (float?)x.total ?? 0f);
                ViewBag.Depositos = entity.Factura.Include("venta").Where(x => x.Venta.idCuadre == cuadreActual && x.numPago == 3 && x.TieneCedito == false).DefaultIfEmpty(null).Sum(x => (float?)x.total ?? 0f);
                ViewBag.TarjetaCredito = entity.Factura.Include("venta").Where(x => x.Venta.idCuadre == cuadreActual &&  x.numPago == 2 && x.TieneCedito == false).DefaultIfEmpty(null).Sum(x => (float?)x.total ?? 0f);
                ViewBag.Credito = entity.Factura.Include("venta").Where(x => x.Venta.idCuadre == cuadreActual &&  x.TieneCedito == true).DefaultIfEmpty(null).Sum(x => (float?)x.total ?? 0f);
                ViewBag.PagosCredito = entity.Pagos.Where(x => x.idCuadre == cuadreActual).DefaultIfEmpty(null).Sum(x => (float?)x.Monto ?? 0f);
                ViewBag.Cortesia = entity.Factura.Include("venta").Include("detalleventa").Where(x => x.Venta.idCuadre == cuadreActual && x.numPago == 0 && x.TieneCedito == false).DefaultIfEmpty(null).Sum(x => (float?)x.Venta.total ?? 0f);


                IEnumerable<Factura> detalleCortesia = entity.Factura.Where(x => x.Venta.idCuadre == cuadreActual &&  x.numPago == 0  && x.TieneCedito == false).ToList();

                var buscarcortesia = entity.DetalleVenta.ToList().Where(x => detalleCortesia.Select(b => b.idVenta).Contains(x.idVenta));

                var buscarcortesiaEspecial = entity.DetallesVentaEspecial.ToList().Where(x => detalleCortesia.Select(b => b.idVenta).Contains(x.idVenta));

                decimal total = 0;

                //foreach (var item in buscarcortesia)
                //{
                //    total += Convert.ToDecimal(item.cantidad * item.precioEntrada);

                //}
                //foreach (var item in buscarcortesiaEspecial)
                //{
                //    total += Convert.ToDecimal(item.cantidad * item.precioEntrada);

                //}


                ViewBag.CortesiaReal = total;

                var DescuentoF = entity.Factura.Include("venta").Where(x => x.descuento != null && x.Venta.idCuadre == cuadreActual).DefaultIfEmpty(null).Sum(x => (float?)x.total ?? 0f);
                var DescuentoV = entity.Factura.Include("venta").Where(x => x.descuento != null && x.Venta.idCuadre == cuadreActual).DefaultIfEmpty(null).Sum(x => (float?)x.Venta.total ?? 0f);

                ViewBag.Descuento = DescuentoV - DescuentoF;

                ViewBag.EnvioCredito = entity.Factura.Include("venta").Where(x => x.Venta.idCuadre == cuadreActual && x.TieneCedito == true).DefaultIfEmpty(null).Sum(x => (float?)x.total ?? 0f);
                //var descuento = entity.Factura.Include("venta").Where(x => x.descuento != null && x.Venta.idCuadre == cuadreActual).DefaultIfEmpty(null).Sum(x => (float?)x.total??0f) /100;
                //ViewBag.Descuento = descuento.ToString();

                var queryInventarioBAR = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleBAR");

                ViewData["agotadosCantidad"] = queryInventarioBAR.Where(x => x.Cantidad <= 3).Count().ToString();

                var Agotados = queryInventarioBAR.Where(x => x.Cantidad <= 3);

                ViewData["agotados"] = Agotados.ToList();
            }

            return View();
        }
        private Venta UntrackedVenta(Venta venta)
        {
            return new Venta
            {
                fecha = venta.fecha,
                idCliente = venta.idCliente,
                idUsuario = venta.idUsuario,
                idVenta = venta.idVenta,
                IVA = venta.IVA,
                ordenCerrada = venta.ordenCerrada,
                ordenFacturada = venta.ordenFacturada,
                total = venta.total
            };
        }
        private DetalleVenta UntrackedDetalle(DetalleVenta detalleVenta)
        {
            return new DetalleVenta
            {
                cantidad = detalleVenta.cantidad,
                despachada = detalleVenta.despachada,
                espcial = detalleVenta.espcial,
                idDetalle = detalleVenta.idDetalle,
                idProducto = detalleVenta.idProducto,
                idVenta = detalleVenta.idVenta,
                precioEntrada = detalleVenta.precioEntrada,
                precioVenta = detalleVenta.precioVenta,
                subTotal = detalleVenta.subTotal
            };
        }
        private Usuario UntrackedUsuario(Usuario usuario)
        {
            return new Usuario
            {
                activo = usuario.activo,
                contrasena = usuario.contrasena,
                Correo = usuario.Correo,
                EnvioCorreo = usuario.EnvioCorreo,
                idRol = usuario.idRol,
                idTarjeta = usuario.idTarjeta,
                idUsuario = usuario.idUsuario,
                nombre = usuario.nombre,
                resetContrasena = usuario.resetContrasena
            };
        }
        #endregion

        #region Home



        //cuadrar
        [HttpPost]
        public ActionResult Cuadrar(string Eectivo, string Deposito, string Tarjeta, string Nota, string VentasCuadre, string cortesiaReal, string PagosCredito)
        {
            try
            {
                using (barbdEntities context = new barbdEntities())
                {
                    if (context.Venta.AsEnumerable().Any(v => !v.ordenFacturada.GetValueOrDefault(false)))
                    {
                        InfoMensaje _Info = new InfoMensaje
                        {
                            Tipo = "Notificacion",
                            Mensaje = "No se puede cuadrar con cuentas abiertas.\nFacture las cuentas abiertas he intente nuevamente."
                        };

                        return Json(_Info, JsonRequestBehavior.DenyGet);
                    }

                    IDictionary<string, string> header = new Dictionary<string, string>();
                    header.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy"));
                    header.Add("Hora", DateTime.Now.ToString("hh:mm:ss tt"));



                    IDictionary<string, string> CuadreGenerar = new Dictionary<string, string>();
                    CuadreGenerar.Add("Venta General: ", VentasCuadre);


                    List<arqueo> ListaArqueo = new List<arqueo>();

                    ListaArqueo.Add(new arqueo { Descripcion = "Eectivo", Valor = string.IsNullOrEmpty(Eectivo) ? 0 : Convert.ToDecimal(Eectivo) });
                    ListaArqueo.Add(new arqueo { Descripcion = "Tarjeta", Valor = string.IsNullOrEmpty(Tarjeta) ? 0 : Convert.ToDecimal(Tarjeta) });
                    ListaArqueo.Add(new arqueo { Descripcion = "Deposito", Valor = string.IsNullOrEmpty(Deposito) ? 0 : Convert.ToDecimal(Deposito) });
                    ListaArqueo.Add(new arqueo { Descripcion = "Creditos Pagos", Valor = string.IsNullOrEmpty(PagosCredito) ? 0 : Convert.ToDecimal(PagosCredito) });

                    string[][] Arqueo =
                       ListaArqueo
                       .Select(r =>
                           new string[2]
                           {
                                r.Descripcion,
                                r.Valor.ToString("$#,0.00")
                           }
                       ).ToArray();


                    string[][] resumen =
                        context.Database.SqlQuery<CuadreResumen>("exec sp_cuadre_resumen")
                        .Select(r =>
                            new string[2]
                            {
                                r.Descripcion,
                                r.Valor.ToString("$#,0.00")
                            }
                        ).ToArray();

                    resumen[5][1] = cortesiaReal;



                    int? idCuadre = context.Cuadre.AsEnumerable().SingleOrDefault(c => c.cerrado.GetValueOrDefault(false) == false).idCuadre;
                    Gastos[] gastos = context.Gastos.Where(g => g.idCuadre == idCuadre).ToArray();
                    Dictionary<string, string> gastosResumen = new Dictionary<string, string>();
                    gastosResumen.Add("Total de gastos" + gastos.Sum(g => g.cantidad).GetValueOrDefault(0).ToString("$#,0.00"), "");

                    IEnumerable<CuadreTable> cuadreBar = context.Database.SqlQuery<CuadreTable>("exec sp_cuadre_bar");
                    Dictionary<string, string> barResumen = new Dictionary<string, string>();
                    //barResumen.Add("Productos vendidos", cuadreBar.Sum(g => g.Cantidad).ToString());
                    barResumen.Add("Total:" + cuadreBar.Sum(g => g.Total).ToString("$#,0.00") + "Beneficios:" + cuadreBar.Sum(g => g.Beneficio).ToString("$#,0.00"), "");
                    //barResumen.Add("Total costos", cuadreBar.Sum(g => g.Costo).ToString("$#,0.00"));
                    //barResumen.Add("Total beneficios", cuadreBar.Sum(g => g.Beneficio).ToString("$#,0.00"));


                    IEnumerable<CuadreTable> cuadreBarH = context.Database.SqlQuery<CuadreTable>("exec sp_cuadre_bar_Hookah");
                    Dictionary<string, string> barResumenH = new Dictionary<string, string>();
                    //barResumen.Add("Productos vendidos", cuadreBar.Sum(g => g.Cantidad).ToString());
                    barResumenH.Add("Total:" + cuadreBarH.Sum(g => g.Total).ToString("$#,0.00") + "Beneficios:" + cuadreBarH.Sum(g => g.Beneficio).ToString("$#,0.00"), "");
                    //barResumen.Add("Total costos", cuadreBar.Sum(g => g.Costo).ToString("$#,0.00"));
                    //barResumen.Add("Total beneficios", cuadreBar.Sum(g => g.Beneficio).ToString("$#,0.00"));



                    IEnumerable<CuadreTable> cuadreRest = context.Database.SqlQuery<CuadreTable>("exec sp_cuadre_restaurante");
                    Dictionary<string, string> restResumen = new Dictionary<string, string>();
                    //restResumen.Add("Productos vendidos", cuadreRest.Sum(g => g.Cantidad).ToString());
                    restResumen.Add("Total:" + cuadreRest.Sum(g => g.Total).ToString("$#,0.00") + "Beneficios:" + cuadreRest.Sum(g => g.Beneficio).ToString("$#,0.00"), "");
                    //restResumen.Add("Total costos", cuadreRest.Sum(g => g.Costo).ToString("$#,0.00"));
                    //restResumen.Add("Total beneficios", cuadreRest.Sum(g => g.Beneficio).ToString("$#,0.00"));



                    IEnumerable<CuadreTable> cuadreEspecial = context.Database.SqlQuery<CuadreTable>("exec sp_cuadre_Especial");
                    Dictionary<string, string> EspecialResumenEspecial = new Dictionary<string, string>();
                    //EspecialResumenEspecial.Add("Productos vendidos", cuadreEspecial.Sum(g => g.Cantidad).ToString());
                    //  EspecialResumenEspecial.Add("Total+"+ cuadreEspecial.Sum(g => g.Total).ToString("$#,0.00"),"");
                    //EspecialResumenEspecial.Add("Total costos", cuadreEspecial.Sum(g => g.Costo).ToString("$#,0.00"));
                    //EspecialResumenEspecial.Add("Total beneficios", cuadreEspecial.Sum(g => g.Beneficio).ToString("$#,0.00"));



                    IEnumerable<CuadreTable> cuadreEspecial1 = context.Database.SqlQuery<CuadreTable>("exec sp_cuadre_Especial1");
                    Dictionary<string, string> EspecialResumenEspecial1 = new Dictionary<string, string>();
                    //EspecialResumenEspecial.Add("Productos vendidos", cuadreEspecial.Sum(g => g.Cantidad).ToString());
                    EspecialResumenEspecial1.Add("Total" + cuadreEspecial1.Sum(g => g.Total).ToString("$#,0.00"), "");
                    //EspecialResumenEspecial.Add("Total costos", cuadreEspecial.Sum(g => g.Costo).ToString("$#,0.00"));
                    //EspecialResumenEspecial.Add("Total beneficios", cuadreEspecial.Sum(g => g.Beneficio).ToString("$#,0.00"));


                    // var queryInventario = context.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleInventario");


                    string[][] queryInventario =
                     context.Database.SqlQuery<Models.CuadreTable>("exec InventarioDisponibleCosto")
                       .Select(r =>
                           new string[4]
                           {
                                  r.Nombre,
                                 r.Cantidad.ToString("#,0."),
                                r.Total.ToString("$#,0.00"),
                                r.Costo.ToString("$#,0.00")

                           }
                       ).ToArray();


                    string[][] queryInventarioBar =
                   context.Database.SqlQuery<Models.CuadreTable>("exec DisponibleBarCosto")
                     .Select(r =>
                         new string[4]
                         {
                                r.Nombre,
                                 r.Cantidad.ToString("#,0."),
                                r.Total.ToString("$#,0.00"),
                                r.Costo.ToString("$#,0.00")


                         }
                     ).ToArray();




                    string[][] ventaCosto = context.Database.SqlQuery<CuadreTable>("exec sp_VentaPrecioCosto").Select(r =>
                          new string[1]
                          {
                                r.Total.ToString()
                          }
                       ).ToArray();


                    string[][] ProductoEliminado =
                       context.Database.SqlQuery<CuadreTable>("exec sp_ProductoEliminado")
                       .Select(r =>
                           new string[3]
                           {
                                r.Nombre,
                                r.Cantidad.ToString(),
                                r.CodigoProducto
                           }
                       ).ToArray();


                    string vc = VentasCuadre.Replace("$", "").Replace(",", "");
                    string gg = ventaCosto[0][0].ToString();

                    Decimal G = Convert.ToDecimal(vc) - Convert.ToDecimal(gg) - gastos.Sum(g => g.cantidad).GetValueOrDefault(0);

                    IDictionary<string, string> Ganancia = new Dictionary<string, string>();
                    Ganancia.Add("GANANCIAS: ", G.ToString("$#,0.00"));


                    HtmlCorreo correo = new HtmlCorreo();
                    Printer printer = new Printer();

                    var Empresa = context.Configuraciones.Where(x => x.Key == "Empresa").First();

                    printer.AddTitle("Cuadre >>>" + Empresa.Value + "<<<");
                    correo.AddTitle("Cuadre >>>" + Empresa.Value + "<<<");
                    printer.AddSpace(2);
                    correo.AddSpace(1);
                    printer.AddDescriptionList(header, 2);
                    correo.AddDescriptionList(header, 2);
                    printer.AddSpace(2);
                    correo.AddSpace(1);
                    printer.AddDescriptionList(CuadreGenerar, 2);
                    correo.AddDescriptionList(CuadreGenerar, 2);
                    printer.AddSpace(2);
                    correo.AddSpace(1);
                    printer.AddSubtitle("Arqueo");
                    correo.AddSubtitle("Arqueo");
                    printer.AddSpace(2);
                    correo.AddSpace(1);
                    printer.AddTable(new string[2] { "Descripción", "Valor" }, Arqueo);
                    correo.AddTable(new string[2] { "Descripción", "Valor" }, Arqueo);
                    printer.AddSubtitle("Resumen");
                    correo.AddSubtitle("Resumen");
                    printer.AddSpace(2);
                    correo.AddSpace(1);
                    printer.AddTable(new string[2] { "Descripción", "Valor" }, resumen);
                    correo.AddTable(new string[2] { "Descripción", "Valor" }, resumen);
                    printer.AddSpace(5);
                    correo.AddSpace(3);
                    printer.AddSubtitle("Gastos");
                    correo.AddSubtitle("Gastos");
                    printer.AddSpace(2);
                    correo.AddSpace(1);
                    printer.AddTable(new string[2] { "Nombre", "Precio" }, gastos.Select(g => new string[2] { g.descripcion, g.cantidad.GetValueOrDefault(0).ToString("$#,0.00") }).ToArray());
                    correo.AddTable(new string[2] { "Nombre", "Precio" }, gastos.Select(g => new string[2] { g.descripcion, g.cantidad.GetValueOrDefault(0).ToString("$#,0.00") }).ToArray());
                    printer.AddSpace();
                    correo.AddSpace(1);
                    printer.AddTableDetails(gastosResumen, 2);
                    correo.AddTableDetails(gastosResumen, 2);
                    printer.AddSpace(5);
                    correo.AddSpace(3);
                    printer.AddSubtitle("Bar");
                    correo.AddSubtitle("Bar");
                    printer.AddSpace(2);
                    correo.AddSpace(3);
                    printer.AddTable(
                        new string[4] { "Producto", "Cant.", "P/UND", "Subtotal" },
                        cuadreBar.Select(b => new string[5] { b.Nombre.ToUpper(), ".", b.Cantidad.ToString(), b.PrecioVenta.ToString("$#,0.00"), b.Total.ToString("$#,0.00") }).ToArray(),
                        true);
                    correo.AddTable(
                      new string[6] { "Producto", "Cant.", "P/UND", "Subtotal", "Costo", "Beneficios" },
                      cuadreBar.Select(b => new string[6] { b.Nombre.ToUpper(), b.Cantidad.ToString(), b.PrecioVenta.ToString("$#,0.00"), b.Total.ToString("$#,0.00"), b.Costo.ToString("$#,0.00"), b.Beneficio.ToString("$#,0.00") }).ToArray(),
                      true);
                    printer.AddSpace();
                    correo.AddSpace(1);
                    printer.AddTableDetails(barResumen, 6);
                    correo.AddTableDetails(barResumen, 6);
                    printer.AddSpace(5);
                    correo.AddSpace(3);

                    printer.AddSubtitle("Hookah");
                    correo.AddSubtitle("Hookah");
                    printer.AddSpace(2);
                    correo.AddSpace(3);
                    printer.AddTable(
                        new string[4] { "Producto", "Cant.", "P/UND", "Subtotal" },
                      cuadreBarH.Select(b => new string[5] { b.Nombre.ToUpper(), ".", b.Cantidad.ToString(), b.PrecioVenta.ToString("$#,0.00"), b.Total.ToString("$#,0.00") }).ToArray(),
                        true);
                    correo.AddTable(
                      new string[6] { "Producto", "Cant.", "P/UND", "Subtotal", "Costo", "Beneficios" },
                      cuadreBarH.Select(b => new string[6] { b.Nombre.ToUpper(), b.Cantidad.ToString(), b.PrecioVenta.ToString("$#,0.00"), b.Total.ToString("$#,0.00"), b.Costo.ToString("$#,0.00"), b.Beneficio.ToString("$#,0.00") }).ToArray(),
                      true);
                    printer.AddSpace();
                    correo.AddSpace(1);
                    printer.AddTableDetails(barResumenH, 6);
                    correo.AddTableDetails(barResumenH, 6);
                    printer.AddSpace(5);
                    correo.AddSpace(3);


                    printer.AddSubtitle(TipoCategoria);
                    correo.AddSubtitle(TipoCategoria);
                    printer.AddSpace(2);
                    correo.AddSpace(1);
                    printer.AddTable(
                        new string[4] { "Producto", "Cant.", "P/UND", "Subtotal" },
                        cuadreRest.Select(r => new string[5] { r.Nombre.ToUpper(), ".", r.Cantidad.ToString(), r.PrecioVenta.ToString("$#,0.00"), r.Total.ToString("$#,0.00") }).ToArray(),
                        true);
                    correo.AddTable(
                    new string[6] { "Producto", "Cant.", "P/UND", "Subtotal", "Costo", "Beneficio" },
                    cuadreRest.Select(r => new string[6] { r.Nombre.ToUpper(), r.Cantidad.ToString(), r.PrecioVenta.ToString("$#,0.00"), r.Total.ToString("$#,0.00"), r.Costo.ToString("$#,0.00"), r.Beneficio.ToString("$#,0.00") }).ToArray(),
                    true);
                    printer.AddSpace();
                    correo.AddSpace(1);
                    printer.AddTableDetails(restResumen, 6);
                    correo.AddTableDetails(restResumen, 6);

                    printer.AddSubtitle("Especiales");
                    correo.AddSubtitle("Especiales");
                    printer.AddSpace(2);
                    correo.AddSpace(1);
                    printer.AddTable(
                        new string[4] { "Especial", "Cant.", "P/UND", "Subtotal" },
                        cuadreEspecial1.Select(b => new string[5] { b.Nombre.ToUpper(), ".", b.Cantidad.ToString(), b.PrecioVenta.ToString("$#,0.00"), b.Total.ToString("$#,0.00") }).ToArray(),
                        true);
                    correo.AddTable(
                     new string[4] { "Especial", "Cant.", "P/UND", "Subtotal" },
                     cuadreEspecial1.Select(b => new string[5] { b.Nombre.ToUpper(), b.Cantidad.ToString(), b.PrecioVenta.ToString("$#,0.00"), b.Total.ToString("$#,0.00"), "." }).ToArray(),
                     true);
                    printer.AddSpace();
                    correo.AddSpace(1);
                    printer.AddTableDetails(EspecialResumenEspecial1, 6);
                    correo.AddTableDetails(EspecialResumenEspecial1, 6);


                    printer.AddSubtitle("Especiales Detalle");
                    correo.AddSubtitle("Especiales Detalle");
                    printer.AddSpace(2);
                    correo.AddSpace(1);
                    printer.AddTable(
                        new string[2] { "Especial", "Cant." },
                        cuadreEspecial.Select(b => new string[3] { b.Nombre.ToUpper(), ".", b.Cantidad.ToString() }).ToArray(),
                        true);
                    correo.AddTable(
                   new string[2] { "Especial", "Cant." },
                   cuadreEspecial.Select(b => new string[3] { b.Nombre.ToUpper(), b.Cantidad.ToString(), "." }).ToArray(),
                   true);
                    printer.AddSpace();
                    correo.AddSpace(1);
                    //  printer.AddTableDetails(EspecialResumenEspecial, 6);
                    correo.AddSubtitle("Productos Eliminados");
                    printer.AddSubtitle("Productos Eliminados");
                    correo.AddTable(new string[3] { "Producto", "Cant.", "No.Orden" }, ProductoEliminado);
                    printer.AddTable(new string[3] { "Producto", "Cant.", "No.Orden" }, ProductoEliminado);
                    correo.AddSubtitle("Inventario Almacen");
                    //printer.AddSubtitle("Inventario Almacen");
                    correo.AddTable(new string[4] { "Producto", "Cantidad", "Precio", "Total" }, queryInventario);
                    //printer.AddTable(new string[4] { "Producto", "Cantidad", "Precio", "Total" }, queryInventario);
                    correo.AddSubtitle("Inventario Bar");
                    //printer.AddSubtitle("Inventario Bar");
                    correo.AddTable(new string[4] { "Producto", "Cantidad", "Precio", "Total" }, queryInventarioBar);
                    //printer.AddTable(new string[4] { "Producto", "Cantidad", "Precio", "Total" }, queryInventarioBar);
                    printer.AddSpace(2);
                    correo.AddSpace(1);
                    printer.AddDescriptionList(Ganancia, 2);
                    correo.AddDescriptionList(Ganancia, 2);



                    printer.AddSubtitle("Nota: " + Nota);
                    correo.AddSubtitle("Nota: " + Nota);

                    printer.AddSpace(5);


                    //string dd = printer.ToString();

                    // printer.AddSubtitle("Nota: " + Nota);

                    printer.Print("Cuadre", correo.html());

                    Cuadre cuadre = context.Cuadre.Find(idCuadre);
                    cuadre.cerrado = true;
                    context.SaveChanges();



                    InfoMensaje Info = new InfoMensaje
                    {
                        Tipo = "Ready",
                        Mensaje = "Cuadre realizado con exito"
                    };

                    return Json(Info, JsonRequestBehavior.DenyGet);
                }
            }
            catch (Exception ex)
            {
                InfoMensaje Info = new InfoMensaje
                {
                    Tipo = "Error",
                    Mensaje = ex.Message
                };

                return Json(Info, JsonRequestBehavior.DenyGet);
            }
        }

        //facturar cuentas
        public ActionResult FacturarCuenta(Usuario usuario)
        {
            using (var entity = new barbdEntities())
            {
                var ListCliente = new List<Cliente>();
                var Clientes = entity.Venta.AsEnumerable().Where(x => x.idUsuario == usuario.idUsuario && x.ordenCerrada == true && !x.ordenFacturada.GetValueOrDefault(false)).ToList();

                foreach (var item in Clientes)
                {
                    var ObjCliente = new Cliente()
                    {
                        idCliente = item.idVenta,
                        nombre = "Orden No." + item.idVenta + " -- " + item.Cliente.nombre.ToUpper() + " -- " + item.total.ToString("$#,0.00")
                    };

                    ListCliente.Add(ObjCliente);
                }

                return Json(ListCliente, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult FacturarCuentaNoOrden(Venta venta)
        {
            using (var entity = new barbdEntities())
            {
                var ListCliente = new List<Cliente>();
                var Clientes = entity.Venta.AsEnumerable().Where(x => x.idVenta == venta.idVenta && x.ordenCerrada == true && !x.ordenFacturada.GetValueOrDefault(false)).ToList();

                foreach (var item in Clientes)
                {
                    var ObjCliente = new Cliente()
                    {
                        idCliente = item.idVenta,
                        nombre = "Orden No." + item.idVenta + " -- " + item.Cliente.nombre.ToUpper() + " -- " + item.total.ToString("$#,0.00")
                    };

                    ListCliente.Add(ObjCliente);
                }

                return Json(ListCliente, JsonRequestBehavior.AllowGet);
            }
        }

        //facturar cuentas
        [HttpPost]
        public ActionResult FacturarReady(RequestVenta venta)
        {
            try
            {

                if (propina_.ToUpper() == "NO")
                {

                    string empresa;
                    string rnc;
                    string telefono;
                    string saludo;
                    string cliente;
                    string vendedor;
                    decimal subtotal;
                    decimal descuentos;
                    decimal itbis;
                    decimal propina;
                    string[][] data;
                    decimal Dollar;
                    decimal Euro;

                    using (barbdEntities context = new barbdEntities())
                    {
                        empresa = context.Configuraciones.Find("Empresa").Value;
                        rnc = String.IsNullOrEmpty(venta.Factura[0].rnc) ? "" : venta.Factura[0].rnc.ToString(); //context.Configuraciones.Find("RNC").Value;
                        telefono = context.Configuraciones.Find("Telefono").Value;
                        saludo = context.Configuraciones.Find("Saludo").Value;
                        var d = context.Configuraciones.Find("Dollar").Value;
                        Dollar = Convert.ToDecimal(d);
                        var e = context.Configuraciones.Find("Euro").Value;
                        Euro = Convert.ToDecimal(e);

                        Venta _venta = context.Venta.Find(venta.idVenta);
                        _venta.ordenFacturada = true;



                        for (int index = 0; index < venta.Factura.Length; index++)
                        {
                            Factura factura = new Factura()
                            {
                                descuento = venta.Factura[index].descuento,
                                fecha = DateTime.Now,
                                IVA = 18,
                                idVenta = venta.Factura[index].idVenta,
                                numFactura = venta.Factura[index].numFactura,
                                numPago = venta.Factura[index].numPago,
                                TieneCedito = false,
                                total = venta.Factura[index].total
                            };

                            if (venta.Factura[index].idUsuario != 0)
                            {
                                factura.TieneCedito = true;

                                Creditos credito = new Creditos()
                                {
                                    idUsuario = venta.Factura[index].idUsuario,
                                    MontoRestante = (decimal)factura.total,
                                    numFactura = factura.numFactura
                                };

                                context.Creditos.Add(credito);
                            }

                            context.Factura.Add(factura);
                        }

                        context.SaveChanges();

                        cliente = string.IsNullOrEmpty(context.Cliente.Find(_venta.idCliente).nMesa) ? "0" : context.Cliente.Find(_venta.idCliente).nMesa;
                        vendedor = context.Usuario.Find(_venta.idUsuario).nombre;
                        subtotal = (decimal)context.DetalleVenta.Where(vd => vd.idVenta == venta.idVenta).Sum(vd => vd.subTotal);
                        descuentos = subtotal - (decimal)venta.Factura.Sum(f => f.total);
                        itbis = subtotal * 0.18m;
                        //propina = subtotal * 0.10m;
                        data =
                            context.DetalleVenta
                            .Where(vd => vd.idVenta == venta.idVenta)
                            .AsEnumerable()
                           .Select(vd => new string[5] {
                        "",
                        vd.Producto.nombre.ToUpper(),
                        Math.Round((decimal)vd.cantidad, 2).ToString("#,0"),
                        Math.Round((decimal)vd.precioVenta, 2).ToString("$#,0.00"),
                        Math.Round((decimal)vd.subTotal, 2).ToString("$#,0.00")
                        })
                        .ToArray();

                        Printer printer = new Printer();

                        IDictionary<string, string> list1 = new Dictionary<string, string>();
                        list1.Add("Cliente", cliente.ToUpper());
                        list1.Add("Orden", venta.idVenta.ToString());
                        list1.Add("Vendedor/a", vendedor.ToUpper());
                        list1.Add("RNC", rnc);
                        if (string.IsNullOrEmpty(rnc))
                        {
                            list1.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy hh:mm:ss tt"));
                            var NCF_ = context.ComprobanteFiscal.Where(x => x.Utilizado == false).Count() > 0 ? context.ComprobanteFiscal.Where(x => x.Utilizado == false).First().NCF : "";
                            list1.Add("NCF", NCF_);

                            var c = context.Cliente.Find(_venta.idCliente);
                            c.ncf = NCF_;
                            c.rnc = rnc;
                            context.SaveChanges();

                            if (!String.IsNullOrEmpty(NCF_))
                            {
                                var ACTRNC = context.ComprobanteFiscal.Where(X => X.NCF == NCF_).First();
                                ACTRNC.Utilizado = true;
                                context.SaveChanges();
                            }
                        }
                        else
                        {
                            list1.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy hh:mm:ss tt"));
                            var NCF_ = context.ComprobanteFiscal.Where(x => x.Utilizado == false).Count() > 0 ? context.ComprobanteFiscal.Where(x => x.Utilizado == false).First().NCF : "";
                            list1.Add("NCF", NCF_);

                            var c = context.Cliente.Find(_venta.idCliente);
                            c.ncf = NCF_;
                            c.rnc = rnc;
                            context.SaveChanges();

                            if (!String.IsNullOrEmpty(NCF_))
                            {
                                var ACTRNC = context.ComprobanteFiscal.Where(X => X.NCF == NCF_).First();
                                ACTRNC.Utilizado = true;
                                context.SaveChanges();
                            }


                        }


                        Dictionary<string, string> tableDetails = new Dictionary<string, string>();
                        Dictionary<string, string> tableTotal = new Dictionary<string, string>();

                        if (itbis_.ToUpper() == "SI")
                        {

                            tableDetails.Add("Subtotal", (subtotal - itbis).ToString("$#,0.00"));
                            //tableDetails.Add("Subtotal", (subtotal - itbis-propina).ToString("$#,0.00"));
                            tableDetails.Add("ITBIS %18", itbis.ToString("$#,0.00"));
                            //tableDetails.Add("Propina %10", propina.ToString("$#,0.00"));
                            tableDetails.Add("Descuento", descuentos.ToString("$#,0.00") + " (" + venta.Factura[0].descuento.GetValueOrDefault(0).ToString() + "%)");
                         
                            tableTotal.Add("TOTAL", (subtotal - descuentos).ToString("$#,0.00"));

                        }
                        else
                        {
                            tableDetails.Add("Descuento", descuentos.ToString("$#,0.00") + " (" + venta.Factura[0].descuento.GetValueOrDefault(0).ToString() + "%)");
                            tableTotal.Add("TOTAL", (subtotal - descuentos).ToString("$#,0.00"));

                        }

                        printer.AddTitle("Factura");
                        printer.AddSpace(2);
                        printer.AddString(empresa, true, System.Drawing.StringAlignment.Center);
                        printer.AddString(telefono, alignment: System.Drawing.StringAlignment.Center);
                        printer.AddSpace(2);
                        printer.AddSubtitle("Información general");
                        printer.AddSpace();
                        printer.AddDescriptionList(list1, 2);
                        printer.AddSpace(2);
                        printer.AddSubtitle("Productos");
                        printer.AddSpace();
                        printer.AddTable(new string[4] { "Código", "Cant.", "Precio", "Subtotal" }, data, true, map: new float[4] { 35f, 15f, 25f, 25f });
                        printer.AddSpace();
                        printer.AddTableDetails(tableDetails, 4);
                        printer.AddSpace();
                        printer.AddTableDetails(tableTotal, 4);
                        printer.AddSpace(2);
                        printer.AddBarCode(venta.idVenta.ToString());
                        printer.AddString(saludo, alignment: System.Drawing.StringAlignment.Center);
                        printer.AddSpace(2);
                        decimal DollarInfo = subtotal / Dollar;
                        printer.AddString("Dollar $ " + DollarInfo.ToString("$#,0.00"), alignment: System.Drawing.StringAlignment.Center);
                        decimal EuroInfo = subtotal / Euro;
                        printer.AddString("Euro $ " + EuroInfo.ToString("$#,0.00"), alignment: System.Drawing.StringAlignment.Center);


                        if (venta.Factura[0].numPago == -1)
                        {
                            printer.AddString("Pago Combinado");

                        }
                        else if (!string.IsNullOrEmpty(venta.Factura[0].idUsuario.ToString()))
                        {
                            printer.AddString("Credito");
                        }
                        else if (venta.Factura[0].numPago == 0)
                        {
                            printer.AddString("Cortesia");
                        }
                        else if (venta.Factura[0].numPago == 1)
                        {
                            printer.AddString("Pago Efectivo");
                        }

                        else if (venta.Factura[0].numPago == 2)
                        {
                            printer.AddString("Pago Tarjeta");
                        }

                        else if (venta.Factura[0].numPago == 3)
                        {
                            printer.AddString("Pago Deposito");
                        }



                        printer.Print(venta.Factura[0].numPago.ToString());

                        InfoMensaje Info = new InfoMensaje
                        {
                            Tipo = "Ready",
                            Mensaje = "Facturación realizada con exito"
                        };

                        return Json(Info, JsonRequestBehavior.AllowGet);

                    }
                }
                else
                {
                    string empresa;
                    string rnc;
                    string telefono;
                    string saludo;
                    string cliente;
                    string vendedor;
                    decimal subtotal;
                    decimal descuentos;
                    decimal itbis;
                    decimal propina;
                    string[][] data;
                    decimal Dollar;
                    decimal Euro;

                    using (barbdEntities context = new barbdEntities())
                    {
                        empresa = context.Configuraciones.Find("Empresa").Value;
                        rnc = String.IsNullOrEmpty(venta.Factura[0].rnc) ? "" : venta.Factura[0].rnc.ToString(); //context.Configuraciones.Find("RNC").Value;
                        telefono = context.Configuraciones.Find("Telefono").Value;
                        saludo = context.Configuraciones.Find("Saludo").Value;
                        var d = context.Configuraciones.Find("Dollar").Value;
                        Dollar = Convert.ToDecimal(d);
                        var e = context.Configuraciones.Find("Euro").Value;
                        Euro = Convert.ToDecimal(e);

                        Venta _venta = context.Venta.Find(venta.idVenta);
                        _venta.ordenFacturada = true;



                        for (int index = 0; index < venta.Factura.Length; index++)
                        {
                            Factura factura = new Factura()
                            {
                                descuento = venta.Factura[index].descuento,
                                fecha = DateTime.Now,
                                IVA = 18,
                                idVenta = venta.Factura[index].idVenta,
                                numFactura = venta.Factura[index].numFactura,
                                numPago = venta.Factura[index].numPago,
                                TieneCedito = false,
                                total = venta.Factura[index].total
                            };

                            if (venta.Factura[index].idUsuario != 0)
                            {
                                factura.TieneCedito = true;

                                Creditos credito = new Creditos()
                                {
                                    idUsuario = venta.Factura[index].idUsuario,
                                    MontoRestante = (decimal)factura.total,
                                    numFactura = factura.numFactura
                                };

                                context.Creditos.Add(credito);
                            }

                            context.Factura.Add(factura);
                        }

                        context.SaveChanges();

                        cliente = string.IsNullOrEmpty(context.Cliente.Find(_venta.idCliente).nMesa) ? "0" : context.Cliente.Find(_venta.idCliente).nMesa;
                        vendedor = context.Usuario.Find(_venta.idUsuario).nombre;
                        subtotal = (decimal)context.DetalleVenta.Where(vd => vd.idVenta == venta.idVenta).Sum(vd => vd.subTotal);
                        descuentos = subtotal - (decimal)venta.Factura.Sum(f => f.total);
                        itbis = subtotal * 0.18m;
                        propina = subtotal * 0.10m;
                        data =
                            context.DetalleVenta
                            .Where(vd => vd.idVenta == venta.idVenta)
                            .AsEnumerable()
                           .Select(vd => new string[5] {
                        "",
                        vd.Producto.nombre.ToUpper(),
                        Math.Round((decimal)vd.cantidad, 2).ToString("#,0"),
                        Math.Round((decimal)vd.precioVenta, 2).ToString("$#,0.00"),
                        Math.Round((decimal)vd.subTotal, 2).ToString("$#,0.00")
                        })
                        .ToArray();

                        Printer printer = new Printer();

                        IDictionary<string, string> list1 = new Dictionary<string, string>();
                        list1.Add("Cliente", cliente.ToUpper());
                        list1.Add("Orden", venta.idVenta.ToString());
                        list1.Add("Vendedor/a", vendedor.ToUpper());
                        list1.Add("RNC", rnc);
                        if (string.IsNullOrEmpty(rnc))
                        {
                            list1.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy hh:mm:ss tt"));
                            var NCF_ = context.ComprobanteFiscal.Where(x => x.Utilizado == false).Count() > 0 ? context.ComprobanteFiscal.Where(x => x.Utilizado == false).First().NCF : "";
                            list1.Add("NCF", NCF_);

                            var c = context.Cliente.Find(_venta.idCliente);
                            c.ncf = NCF_;
                            c.rnc = rnc;
                            context.SaveChanges();

                            if (!String.IsNullOrEmpty(NCF_))
                            {
                                var ACTRNC = context.ComprobanteFiscal.Where(X => X.NCF == NCF_).First();
                                ACTRNC.Utilizado = true;
                                context.SaveChanges();
                            }
                        }
                        else
                        {
                            list1.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy hh:mm:ss tt"));
                            var NCF_ = context.ComprobanteFiscal.Where(x => x.Utilizado == false).Count() > 0 ? context.ComprobanteFiscal.Where(x => x.Utilizado == false).First().NCF : "";
                            list1.Add("NCF", NCF_);

                            var c = context.Cliente.Find(_venta.idCliente);
                            c.ncf = NCF_;
                            c.rnc = rnc;
                            context.SaveChanges();

                            if (!String.IsNullOrEmpty(NCF_))
                            {
                                var ACTRNC = context.ComprobanteFiscal.Where(X => X.NCF == NCF_).First();
                                ACTRNC.Utilizado = true;
                                context.SaveChanges();
                            }


                        }





                        Dictionary<string, string> tableDetails = new Dictionary<string, string>();
                        tableDetails.Add("Subtotal", (subtotal - itbis - propina).ToString("$#,0.00"));
                        //tableDetails.Add("Subtotal", (subtotal - itbis-propina).ToString("$#,0.00"));
                        tableDetails.Add("ITBIS %18", itbis.ToString("$#,0.00"));
                        tableDetails.Add("Propina %10", propina.ToString("$#,0.00"));
                        tableDetails.Add("Descuento", descuentos.ToString("$#,0.00") + " (" + venta.Factura[0].descuento.GetValueOrDefault(0).ToString() + "%)");
                        Dictionary<string, string> tableTotal = new Dictionary<string, string>();
                        tableTotal.Add("TOTAL", (subtotal - descuentos).ToString("$#,0.00"));

                        printer.AddTitle("Factura");
                        printer.AddSpace(2);
                        printer.AddString(empresa, true, System.Drawing.StringAlignment.Center);
                        printer.AddString(telefono, alignment: System.Drawing.StringAlignment.Center);
                        printer.AddSpace(2);
                        printer.AddSubtitle("Información general");
                        printer.AddSpace();
                        printer.AddDescriptionList(list1, 2);
                        printer.AddSpace(2);
                        printer.AddSubtitle("Productos");
                        printer.AddSpace();
                        printer.AddTable(new string[4] { "Código", "Cant.", "Precio", "Subtotal" }, data, true, map: new float[4] { 35f, 15f, 25f, 25f });
                        printer.AddSpace();
                        printer.AddTableDetails(tableDetails, 4);
                        printer.AddSpace();
                        printer.AddTableDetails(tableTotal, 4);
                        printer.AddSpace(2);
                        printer.AddBarCode(venta.idVenta.ToString());
                        printer.AddString(saludo, alignment: System.Drawing.StringAlignment.Center);
                        printer.AddSpace(2);
                        decimal DollarInfo = subtotal / Dollar;
                        printer.AddString("Dollar $ " + DollarInfo.ToString("$#,0.00"), alignment: System.Drawing.StringAlignment.Center);
                        decimal EuroInfo = subtotal / Euro;
                        printer.AddString("Euro $ " + EuroInfo.ToString("$#,0.00"), alignment: System.Drawing.StringAlignment.Center);


                        if (venta.Factura[0].numPago == -1)
                        {
                            printer.AddString("Pago Combinado");

                        }
                        else if (!string.IsNullOrEmpty(venta.Factura[0].idUsuario.ToString()))
                        {
                            printer.AddString("Credito");
                        }
                        else if (venta.Factura[0].numPago == 0)
                        {
                            printer.AddString("Cortesia");
                        }
                        else if (venta.Factura[0].numPago == 1)
                        {
                            printer.AddString("Pago Efectivo");
                        }

                        else if (venta.Factura[0].numPago == 2)
                        {
                            printer.AddString("Pago Tarjeta");
                        }

                        else if (venta.Factura[0].numPago == 3)
                        {
                            printer.AddString("Pago Deposito");
                        }



                        printer.Print(venta.Factura[0].numPago.ToString());

                        InfoMensaje Info = new InfoMensaje
                        {
                            Tipo = "Ready",
                            Mensaje = "Facturación realizada con exito"
                        };

                        return Json(Info, JsonRequestBehavior.AllowGet);

                    }
                }
            }
            catch (Exception ex)
            {
                InfoMensaje Info = new InfoMensaje
                {
                    Tipo = "Error",
                    Mensaje = ex.Message
                };

                return Json(Info, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public ActionResult Prefacturarr(int id)
        {

            //using (barbdEntities context1 = new barbdEntities())
            //{

            //    int ValidarDetalles = context1.DetalleVenta.Where(x => x.idVenta == id).Count();

            //    if (ValidarDetalles == 0)
            //    {

            //        var V = context1.Venta.Find(id);

            //        V.ordenCerrada = true;
            //        V.ordenFacturada = true;

            //        context1.SaveChanges();

            //        Venta _venta = context1.Venta.Find(id);

            //        Factura factura = new Factura()
            //        {

            //            fecha = DateTime.Now,
            //            IVA = 18,
            //            idVenta = _venta.idVenta,
            //            // numFactura = venta.Factura[index].numFactura,
            //            numPago = 1,
            //            TieneCedito = false,
            //            total = 0
            //        };
            //        context1.Factura.Add(factura);
            //        context1.SaveChanges();

            //        return 0;

            //    }


            //}
            if (propina_.ToUpper() == "NO")
            {

                int result = 0;
                string empresa;
                string rnc;
                string telefono;
                string saludo;
                string cliente;
                string vendedor;
                decimal subtotal;
                decimal itbis;
                string[][] data;

                using (barbdEntities context = new barbdEntities())
                {
                    empresa = context.Configuraciones.Find("Empresa").Value;
                    rnc = context.Configuraciones.Find("RNC").Value;
                    telefono = context.Configuraciones.Find("Telefono").Value;
                    saludo = context.Configuraciones.Find("Saludo").Value;

                    Venta venta = context.Venta.Find(id);
                    venta.ordenCerrada = true;

                    venta.Cliente.idMesa = null;

                    result = context.SaveChanges();
                    cliente = "xxx";//context.Cliente.Find(venta.idCliente).nombre;
                    vendedor = context.Usuario.Find(venta.idUsuario).nombre;
                    subtotal = (decimal)context.DetalleVenta.Where(vd => vd.idVenta == id).Sum(vd => vd.subTotal);
                    itbis = subtotal * 0.18m;
                    data =
                        context.DetalleVenta
                        .Where(vd => vd.idVenta == id)
                        .AsEnumerable()
                        .Select(vd => new string[5] {
                        "",
                        vd.Producto.nombre.ToUpper(),
                        Math.Round((decimal)vd.cantidad, 2).ToString("#,0"),
                        Math.Round((decimal)vd.precioVenta, 2).ToString("$#,0.00"),
                        Math.Round((decimal)vd.subTotal, 2).ToString("$#,0.00")
                        })
                        .ToArray();
                }

                Printer printer = new Printer();

                IDictionary<string, string> list1 = new Dictionary<string, string>();
                list1.Add("Cliente", cliente.ToUpper());
                list1.Add("Orden", id.ToString());
                list1.Add("Vendedor/a", vendedor.ToUpper());
                list1.Add("RNC", rnc);
                list1.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy"));
                list1.Add("Hora", DateTime.Now.ToString("hh:mm:ss tt"));

                Dictionary<string, string> tableDetails = new Dictionary<string, string>();
                Dictionary<string, string> tableTotal = new Dictionary<string, string>();

                if (itbis_.ToUpper() == "SI")
                {

                    tableDetails.Add("Subtotal", (subtotal - itbis).ToString("$#,0.00"));
                    tableDetails.Add("ITBIS", itbis.ToString("$#,0.00"));
                    tableTotal.Add("TOTAL", subtotal.ToString("$#,0.00"));
                }
                else
                {
                    tableTotal.Add("TOTAL", subtotal.ToString("$#,0.00"));
                }

                printer.AddTitle("Prefactura");
                printer.AddSpace(2);
                printer.AddString(empresa, true, System.Drawing.StringAlignment.Center);
                printer.AddString(telefono, alignment: System.Drawing.StringAlignment.Center);
                printer.AddSpace(2);
                printer.AddSubtitle("Información general");
                printer.AddSpace();
                printer.AddDescriptionList(list1, 2);
                printer.AddSpace(2);
                printer.AddSubtitle("Productos");
                printer.AddSpace();
                printer.AddTable(new string[4] { "Producto", "Cant.", "Precio", "Subtotal" }, data, true, map: new float[4] { 35f, 15f, 25f, 25f });
                printer.AddSpace();
                printer.AddTableDetails(tableDetails, 4);
                printer.AddSpace();
                printer.AddTableDetails(tableTotal, 4);
                printer.AddSpace(2);
                printer.AddBarCode(id.ToString());
                printer.AddString(saludo, alignment: System.Drawing.StringAlignment.Center);
                printer.AddSpace(2);

                printer.Print();

                using (var entity = new barbdEntities())
                {


                    List<Venta> v = new List<Venta>();

                    var idcuadre = entity.Cuadre.SingleOrDefault(x => x.cerrado == false);


                    var ReportesVentas_ = entity.Venta.Include("usuario").Where(x => x.idCuadre == idcuadre.idCuadre && x.ordenFacturada == true).ToList();

                    ViewBag.tipopago = entity.ModoPago.ToList();
                    return PartialView(ReportesVentas_);

                }

            }
            else
            {


                int result = 0;
                string empresa;
                string rnc;
                string telefono;
                string saludo;
                string cliente;
                string vendedor;
                decimal subtotal;
                decimal itbis;
                string[][] data;
                decimal propina;

                using (barbdEntities context = new barbdEntities())
                {
                    empresa = context.Configuraciones.Find("Empresa").Value;
                    rnc = context.Configuraciones.Find("RNC").Value;
                    telefono = context.Configuraciones.Find("Telefono").Value;
                    saludo = context.Configuraciones.Find("Saludo").Value;

                    Venta venta = context.Venta.Find(id);
                    venta.ordenCerrada = true;

                    venta.Cliente.idMesa = null;

                    result = context.SaveChanges();
                    cliente = "xxx";//context.Cliente.Find(venta.idCliente).nombre;
                    vendedor = context.Usuario.Find(venta.idUsuario).nombre;
                    subtotal = (decimal)context.DetalleVenta.Where(vd => vd.idVenta == id).Sum(vd => vd.subTotal);
                    itbis = subtotal * 0.18m;
                    propina = subtotal * 0.10m;
                    data =
                        context.DetalleVenta
                        .Where(vd => vd.idVenta == id)
                        .AsEnumerable()
                        .Select(vd => new string[5] {
                        "",
                        vd.Producto.nombre.ToUpper(),
                        Math.Round((decimal)vd.cantidad, 2).ToString("#,0"),
                        Math.Round((decimal)vd.precioVenta, 2).ToString("$#,0.00"),
                        Math.Round((decimal)vd.subTotal, 2).ToString("$#,0.00")
                        })
                        .ToArray();
                }

                Printer printer = new Printer();

                IDictionary<string, string> list1 = new Dictionary<string, string>();
                list1.Add("Cliente", cliente.ToUpper());
                list1.Add("Orden", id.ToString());
                list1.Add("Vendedor/a", vendedor.ToUpper());
                list1.Add("RNC", rnc);
                list1.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy"));
                list1.Add("Hora", DateTime.Now.ToString("hh:mm:ss tt"));

                Dictionary<string, string> tableDetails = new Dictionary<string, string>();
                tableDetails.Add("Subtotal", (subtotal - itbis - propina).ToString("$#,0.00"));
                tableDetails.Add("ITBIS %18", itbis.ToString("$#,0.00"));
                tableDetails.Add("Propina %10", propina.ToString("$#,0.00"));
                Dictionary<string, string> tableTotal = new Dictionary<string, string>();
                tableTotal.Add("TOTAL", subtotal.ToString("$#,0.00"));

                printer.AddTitle("Prefactura");
                printer.AddSpace(2);
                printer.AddString(empresa, true, System.Drawing.StringAlignment.Center);
                printer.AddString(telefono, alignment: System.Drawing.StringAlignment.Center);
                printer.AddSpace(2);
                printer.AddSubtitle("Información general");
                printer.AddSpace();
                printer.AddDescriptionList(list1, 2);
                printer.AddSpace(2);
                printer.AddSubtitle("Productos");
                printer.AddSpace();
                printer.AddTable(new string[4] { "Producto", "Cant.", "Precio", "Subtotal" }, data, true, map: new float[4] { 35f, 15f, 25f, 25f });
                printer.AddSpace();
                printer.AddTableDetails(tableDetails, 4);
                printer.AddSpace();
                printer.AddTableDetails(tableTotal, 4);
                printer.AddSpace(2);
                printer.AddBarCode(id.ToString());
                printer.AddString(saludo, alignment: System.Drawing.StringAlignment.Center);
                printer.AddSpace(2);

                printer.Print();

                using (var entity = new barbdEntities())
                {


                    List<Venta> v = new List<Venta>();

                    var idcuadre = entity.Cuadre.SingleOrDefault(x => x.cerrado == false);


                    var ReportesVentas_ = entity.Venta.Include("usuario").Where(x => x.idCuadre == idcuadre.idCuadre && x.ordenFacturada == true).ToList();

                    ViewBag.tipopago = entity.ModoPago.ToList();
                    return PartialView(ReportesVentas_);

                }

            }
        }


        [HttpPost]
        public ActionResult FacturarReadyR(int Id)
        {

            List<Factura> FL = new List<Factura>();
            Factura F = new Factura();

            try
            {
                if (propina_.ToUpper() == "NO")
                {

                    string empresa;
                    string rnc;
                    string telefono;
                    string saludo;
                    string cliente;
                    string vendedor;
                    decimal subtotal;
                    decimal descuentos;
                    decimal itbis;
                    string[][] data;

                    using (barbdEntities context = new barbdEntities())
                    {

                        var total = context.Factura.Where(x => x.idVenta == Id).Select(x => x.total).First();
                        var numPago = context.Factura.Where(x => x.idVenta == Id).Select(x => x.numPago).First();
                        var descuento = context.Factura.Where(x => x.idVenta == Id).Select(x => x.descuento).First();
                        var idUsuario = context.Venta.Where(x => x.idVenta == Id).Select(x => x.idUsuario).First();


                        RequestVenta._Factura[] r = new RequestVenta._Factura[] { new RequestVenta._Factura { total = total, numPago = numPago, descuento = descuento, idUsuario = (int)idUsuario } };

                        var venta = new RequestVenta();
                        venta.idVenta = Id;
                        venta.idUsuario = idUsuario;
                        venta.Factura = r;




                        venta.idUsuario = context.Venta.Where(x => x.idVenta == Id).Select(x => x.idUsuario).First();
                        venta.idVenta = context.Venta.Where(x => x.idVenta == Id).Select(x => x.idVenta).First();

                        empresa = context.Configuraciones.Find("Empresa").Value;
                        rnc = context.Configuraciones.Find("RNC").Value;
                        telefono = context.Configuraciones.Find("Telefono").Value;
                        saludo = context.Configuraciones.Find("Saludo").Value;

                        Venta _venta = context.Venta.Find(venta.idVenta);
                        _venta.ordenFacturada = true;

                        //for (int index = 0; index < venta.Factura.Length; index++)
                        //{
                        //    Factura factura = new Factura()
                        //    {
                        //        descuento = venta.Factura[index].descuento,
                        //        fecha = DateTime.Now,
                        //        IVA = 18,
                        //        idVenta = venta.Factura[index].idVenta,
                        //        numFactura = venta.Factura[index].numFactura,
                        //        numPago = venta.Factura[index].numPago,
                        //        TieneCedito = false,
                        //        total = venta.Factura[index].total
                        //    };

                        //    if (venta.Factura[index].idUsuario != 0)
                        //    {
                        //        factura.TieneCedito = true;

                        //        Creditos credito = new Creditos()
                        //        {
                        //            idUsuario = venta.Factura[index].idUsuario,
                        //            MontoRestante = (decimal)factura.total,
                        //            numFactura = factura.numFactura
                        //        };

                        //        context.Creditos.Add(credito);
                        //    }

                        //    context.Factura.Add(factura);
                        //}

                        //context.SaveChanges();

                        cliente = "xxx";///context.Cliente.Find(_venta.idCliente).nombre;
                        vendedor = context.Usuario.Find(_venta.idUsuario).nombre;
                        subtotal = (decimal)context.DetalleVenta.Where(vd => vd.idVenta == venta.idVenta).Sum(vd => vd.subTotal);
                        descuentos = subtotal - (decimal)venta.Factura.Sum(f => f.total);
                        itbis = subtotal * 0.18m;
                        data =
                            context.DetalleVenta
                            .Where(vd => vd.idVenta == venta.idVenta)
                            .AsEnumerable()
                           .Select(vd => new string[5] {
                        "",
                        vd.Producto.nombre.ToUpper(),
                        Math.Round((decimal)vd.cantidad, 2).ToString("#,0"),
                        Math.Round((decimal)vd.precioVenta, 2).ToString("$#,0.00"),
                        Math.Round((decimal)vd.subTotal, 2).ToString("$#,0.00")
                        })
                        .ToArray();

                        Printer printer = new Printer();

                        IDictionary<string, string> list1 = new Dictionary<string, string>();
                        list1.Add("Cliente", cliente.ToUpper());
                        list1.Add("Orden", venta.idVenta.ToString());
                        list1.Add("Vendedor/a", vendedor.ToUpper());
                        list1.Add("RNC", rnc);
                        list1.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy"));
                        list1.Add("Hora", DateTime.Now.ToString("hh:mm:ss tt"));

                        Dictionary<string, string> tableDetails = new Dictionary<string, string>();
                        Dictionary<string, string> tableTotal = new Dictionary<string, string>();

                        if (itbis_.ToUpper() == "SI")
                        {

                           
                            tableDetails.Add("Subtotal", (subtotal - itbis).ToString("$#,0.00"));
                            tableDetails.Add("ITBIS", itbis.ToString("$#,0.00"));
                            tableDetails.Add("Descuento", descuentos.ToString("$#,0.00") + " (" + venta.Factura[0].descuento.GetValueOrDefault(0).ToString() + "%)");
                    
                            tableTotal.Add("TOTAL", (subtotal - descuentos).ToString("$#,0.00"));
                        }
                        else
                        {
                            tableDetails.Add("Descuento", descuentos.ToString("$#,0.00") + " (" + venta.Factura[0].descuento.GetValueOrDefault(0).ToString() + "%)");

                            tableTotal.Add("TOTAL", (subtotal - descuentos).ToString("$#,0.00"));
                        }  



                        printer.AddTitle("Factura de consumidor final");
                        printer.AddSpace(2);
                        printer.AddString(empresa, true, System.Drawing.StringAlignment.Center);
                        printer.AddString(telefono, alignment: System.Drawing.StringAlignment.Center);
                        printer.AddSpace(2);
                        printer.AddSubtitle("Información general");
                        printer.AddSpace();
                        printer.AddDescriptionList(list1, 2);
                        printer.AddSpace(2);
                        printer.AddSubtitle("Productos");
                        printer.AddSpace();
                        printer.AddTable(new string[4] { "Código", "Cantidad", "Precio", "Subtotal" }, data, true, map: new float[4] { 35f, 15f, 25f, 25f });
                        printer.AddSpace();
                        printer.AddTableDetails(tableDetails, 4);
                        printer.AddSpace();
                        printer.AddTableDetails(tableTotal, 4);
                        printer.AddSpace(2);
                        printer.AddBarCode(venta.idVenta.ToString());
                        printer.AddString(saludo, alignment: System.Drawing.StringAlignment.Center);
                        printer.AddSpace(2);

                        printer.Print();

                        InfoMensaje Info = new InfoMensaje
                        {
                            Tipo = "Ready",
                            Mensaje = "Facturación realizada con exito"
                        };

                        using (var entity = new barbdEntities())
                        {


                            List<Venta> v = new List<Venta>();

                            var idcuadre = entity.Cuadre.SingleOrDefault(x => x.cerrado == false);


                            var ReportesVentas_ = entity.Venta.Include("usuario").Where(x => x.idCuadre == idcuadre.idCuadre && x.ordenFacturada == true).ToList();

                            ViewBag.tipopago = entity.ModoPago.ToList();
                            return PartialView(ReportesVentas_);

                        }

                        //  return Json(Info, JsonRequestBehavior.AllowGet);
                    }

                }
                else
                {
                    string empresa;
                    string rnc;
                    string telefono;
                    string saludo;
                    string cliente;
                    string vendedor;
                    decimal subtotal;
                    decimal descuentos;
                    decimal itbis;
                    string[][] data;
                    decimal propina;

                    using (barbdEntities context = new barbdEntities())
                    {

                        var total = context.Factura.Where(x => x.idVenta == Id).Select(x => x.total).First();
                        var numPago = context.Factura.Where(x => x.idVenta == Id).Select(x => x.numPago).First();
                        var descuento = context.Factura.Where(x => x.idVenta == Id).Select(x => x.descuento).First();
                        var idUsuario = context.Venta.Where(x => x.idVenta == Id).Select(x => x.idUsuario).First();


                        RequestVenta._Factura[] r = new RequestVenta._Factura[] { new RequestVenta._Factura { total = total, numPago = numPago, descuento = descuento, idUsuario = (int)idUsuario } };

                        var venta = new RequestVenta();
                        venta.idVenta = Id;
                        venta.idUsuario = idUsuario;
                        venta.Factura = r;




                        venta.idUsuario = context.Venta.Where(x => x.idVenta == Id).Select(x => x.idUsuario).First();
                        venta.idVenta = context.Venta.Where(x => x.idVenta == Id).Select(x => x.idVenta).First();

                        empresa = context.Configuraciones.Find("Empresa").Value;
                        rnc = context.Configuraciones.Find("RNC").Value;
                        telefono = context.Configuraciones.Find("Telefono").Value;
                        saludo = context.Configuraciones.Find("Saludo").Value;

                        Venta _venta = context.Venta.Find(venta.idVenta);
                        _venta.ordenFacturada = true;

                        //for (int index = 0; index < venta.Factura.Length; index++)
                        //{
                        //    Factura factura = new Factura()
                        //    {
                        //        descuento = venta.Factura[index].descuento,
                        //        fecha = DateTime.Now,
                        //        IVA = 18,
                        //        idVenta = venta.Factura[index].idVenta,
                        //        numFactura = venta.Factura[index].numFactura,
                        //        numPago = venta.Factura[index].numPago,
                        //        TieneCedito = false,
                        //        total = venta.Factura[index].total
                        //    };

                        //    if (venta.Factura[index].idUsuario != 0)
                        //    {
                        //        factura.TieneCedito = true;

                        //        Creditos credito = new Creditos()
                        //        {
                        //            idUsuario = venta.Factura[index].idUsuario,
                        //            MontoRestante = (decimal)factura.total,
                        //            numFactura = factura.numFactura
                        //        };

                        //        context.Creditos.Add(credito);
                        //    }

                        //    context.Factura.Add(factura);
                        //}

                        //context.SaveChanges();

                        cliente = "xxx";///context.Cliente.Find(_venta.idCliente).nombre;
                        vendedor = context.Usuario.Find(_venta.idUsuario).nombre;
                        subtotal = (decimal)context.DetalleVenta.Where(vd => vd.idVenta == venta.idVenta).Sum(vd => vd.subTotal);
                        descuentos = subtotal - (decimal)venta.Factura.Sum(f => f.total);
                        itbis = subtotal * 0.18m;
                        propina = subtotal * 0.10m;
                       
                        data =
                            context.DetalleVenta
                            .Where(vd => vd.idVenta == venta.idVenta)
                            .AsEnumerable()
                           .Select(vd => new string[5] {
                        "",
                        vd.Producto.nombre.ToUpper(),
                        Math.Round((decimal)vd.cantidad, 2).ToString("#,0"),
                        Math.Round((decimal)vd.precioVenta, 2).ToString("$#,0.00"),
                        Math.Round((decimal)vd.subTotal, 2).ToString("$#,0.00")
                        })
                        .ToArray();

                        Printer printer = new Printer();

                        IDictionary<string, string> list1 = new Dictionary<string, string>();
                        list1.Add("Cliente", cliente.ToUpper());
                        list1.Add("Orden", venta.idVenta.ToString());
                        list1.Add("Vendedor/a", vendedor.ToUpper());
                        list1.Add("RNC", rnc);
                        list1.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy"));
                        list1.Add("Hora", DateTime.Now.ToString("hh:mm:ss tt"));

                        Dictionary<string, string> tableDetails = new Dictionary<string, string>();
                        tableDetails.Add("Subtotal", (subtotal - itbis - propina).ToString("$#,0.00"));
                        tableDetails.Add("ITBIS %18", itbis.ToString("$#,0.00"));
                        tableDetails.Add("Propina %10", propina.ToString("$#,0.00"));
                        tableDetails.Add("Descuento", descuentos.ToString("$#,0.00") + " (" + venta.Factura[0].descuento.GetValueOrDefault(0).ToString() + "%)");
                        Dictionary<string, string> tableTotal = new Dictionary<string, string>();
                        tableTotal.Add("TOTAL", (subtotal - descuentos).ToString("$#,0.00"));

                        printer.AddTitle("Factura de consumidor final");
                        printer.AddSpace(2);
                        printer.AddString(empresa, true, System.Drawing.StringAlignment.Center);
                        printer.AddString(telefono, alignment: System.Drawing.StringAlignment.Center);
                        printer.AddSpace(2);
                        printer.AddSubtitle("Información general");
                        printer.AddSpace();
                        printer.AddDescriptionList(list1, 2);
                        printer.AddSpace(2);
                        printer.AddSubtitle("Productos");
                        printer.AddSpace();
                        printer.AddTable(new string[4] { "Código", "Cantidad", "Precio", "Subtotal" }, data, true, map: new float[4] { 35f, 15f, 25f, 25f });
                        printer.AddSpace();
                        printer.AddTableDetails(tableDetails, 4);
                        printer.AddSpace();
                        printer.AddTableDetails(tableTotal, 4);
                        printer.AddSpace(2);
                        printer.AddBarCode(venta.idVenta.ToString());
                        printer.AddString(saludo, alignment: System.Drawing.StringAlignment.Center);
                        printer.AddSpace(2);

                        printer.Print();

                        InfoMensaje Info = new InfoMensaje
                        {
                            Tipo = "Ready",
                            Mensaje = "Facturación realizada con exito"
                        };

                        using (var entity = new barbdEntities())
                        {


                            List<Venta> v = new List<Venta>();

                            var idcuadre = entity.Cuadre.SingleOrDefault(x => x.cerrado == false);


                            var ReportesVentas_ = entity.Venta.Include("usuario").Where(x => x.idCuadre == idcuadre.idCuadre && x.ordenFacturada == true).ToList();

                            ViewBag.tipopago = entity.ModoPago.ToList();
                            return PartialView(ReportesVentas_);

                        }

                        //  return Json(Info, JsonRequestBehavior.AllowGet);
                    }

                }
            }
            catch (Exception ex)
            {

                InfoMensaje Info = new InfoMensaje
                {
                    Tipo = "Error",
                    Mensaje = ex.Message
                };
                using (var entity = new barbdEntities())
                {


                    List<Venta> v = new List<Venta>();

                    var idcuadre = entity.Cuadre.SingleOrDefault(x => x.cerrado == false);


                    var ReportesVentas_ = entity.Venta.Include("usuario").Where(x => x.idCuadre == idcuadre.idCuadre && x.ordenFacturada == true).ToList();

                    ViewBag.tipopago = entity.ModoPago.ToList();
                    return PartialView(ReportesVentas_);

                }
                //  return Json(Info, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult DividirBuscarCuenta(int Cuenta)
        {

            using (var entity = new barbdEntities())
            {
                int idVenta = Convert.ToInt32(Cuenta);
                var ListaDetalleVenta = entity.DetalleVenta.Include("Venta").Include("Producto").Where(x => x.idVenta == idVenta).ToList();

                var dll = new List<Cliente>();

                foreach (var item in ListaDetalleVenta)
                {
                    var ObjDetalleVenta = new Cliente()
                    {
                        idCliente = item.idDetalle,
                        nombre = item.Producto.nombre + "-" + item.cantidad.ToString()


                        // idCliente = item.idVenta,
                        //   nombre = "Orden No." + item.idVenta + " -- " + item.nombre.ToUpper() + " -- " + item.total.ToString("$#,0.00")
                    };

                    dll.Add(ObjDetalleVenta);
                }

                return Json(dll, JsonRequestBehavior.AllowGet);
            }


        }

        //transferir cuentas
        public ActionResult TransferirCuenta(Usuario usuario)
        {
            using (var entity = new barbdEntities())
            {
                var ListCliente = new List<Cliente>();
                var Clientes = entity.Venta.Include("Cliente").Where(x => x.idUsuario == usuario.idUsuario && x.ordenCerrada == null).Select(x => new { x.Cliente.idCliente, x.Cliente.nombre, x.idVenta, x.total }).ToList();

                foreach (var item in Clientes)
                {
                    var ObjCliente = new Cliente()
                    {
                        idCliente = item.idVenta,
                        nombre = "Orden No." + item.idVenta + " -- " + item.nombre.ToUpper() + " -- " + item.total.ToString("$#,0.00")
                    };

                    ListCliente.Add(ObjCliente);
                }

                return Json(ListCliente, JsonRequestBehavior.AllowGet);
            }
        }

        //transferir cuentas
        [HttpPost]
        public ActionResult TransferirReady(Venta venta)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    if (venta.idUsuario > 0)
                    {
                        var ObjVenta = entity.Venta.Find(venta.idVenta);
                        ObjVenta.idUsuario = venta.idUsuario;
                        entity.SaveChanges();

                        var Info = new InfoMensaje
                        {
                            Tipo = "Ready",
                            Mensaje = "Tranferencia realizada con exito"

                        };

                        return Json(Info, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Notificacion",
                            Mensaje = "Seleccione camarer@ destino"

                        };

                        return Json(Info, JsonRequestBehavior.AllowGet);
                    }
                }
                catch (Exception ex)
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message

                    };

                    return Json(Info, JsonRequestBehavior.AllowGet);
                }
            }
        }


        public void NuevaOrden(Cliente cliente, int idUsuario, List<string> ListadoDividir)
        {

            using (var Context = new barbdEntities())
            {
                var ObjCliente = Context.Cliente.Add(cliente);
                Context.SaveChanges();

                var ObjVenta = new Venta
                {
                    total = 0,
                    idCliente = ObjCliente.idCliente,
                    fecha = DateTime.Now,
                    IVA = Context.Impuesto.Single().Itbis.Value,
                    idUsuario = idUsuario, //Convert.ToInt32(Session["IdUsuario"]),
                    idCuadre = Context.Cuadre.AsEnumerable().SingleOrDefault(c => !c.cerrado.GetValueOrDefault(false)).idCuadre
                };

                var v = Context.Venta.Add(ObjVenta);
                Context.SaveChanges();

                foreach (var item in ListadoDividir)
                {
                    int idD = Convert.ToInt32(item);

                    var detalleVenta = Context.DetalleVenta.Find(idD);

                    var r = new DetalleVenta()
                    {
                        idVenta = v.idVenta,
                        subTotal = detalleVenta.subTotal,
                        idProducto = detalleVenta.idProducto,
                        cantidad = detalleVenta.cantidad,
                        despachada = detalleVenta.despachada,
                        precioVenta = detalleVenta.precioVenta,
                        precioEntrada = detalleVenta.precioEntrada,
                        espcial = detalleVenta.espcial,
                        idEspecial = detalleVenta.idEspecial
                    };

                    int iddetalleVenta;
                    AgregarProductoCarrito(r, out iddetalleVenta);

                    EliminarProductoCarrito(idD, v.idVenta, iddetalleVenta);

                }

            }

        }
        public void AgregarProductoCarrito(DetalleVenta detalleVenta, out int idDetalle)
        {
            using (var Context = new barbdEntities())
            {

                Producto producto = Context.Producto.Find(detalleVenta.idProducto);

                //////////////Especiales

                if (producto.especial == true)
                {



                    Context.DetalleVenta.Add(detalleVenta);

                    Venta venta = Context.Venta.Find(detalleVenta.idVenta);
                    venta.total += detalleVenta.subTotal;
                    Context.Entry(venta).State = System.Data.Entity.EntityState.Modified;



                    Context.SaveChanges();

                    idDetalle = detalleVenta.idDetalle;





                }

                else
                {



                    Context.DetalleVenta.Add(detalleVenta);

                    Venta venta = Context.Venta.Find(detalleVenta.idVenta);
                    venta.total += detalleVenta.subTotal;
                    Context.Entry(venta).State = System.Data.Entity.EntityState.Modified;



                    Context.SaveChanges();
                    idDetalle = detalleVenta.idDetalle;

                }
            }
        }

        public void EliminarProductoCarrito(int idDetalle, decimal Idventa, int idDetalle_)
        {

            using (barbdEntities context = new barbdEntities())
            {


                var validarEspecial = context.DetallesVentaEspecial.Where(x => x.idDetalle == idDetalle).Count();

                if (validarEspecial > 0)
                {

                    var ddL = new List<DetallesVentaEspecial>();



                    var detalleE = context.DetallesVentaEspecial.Where(x => x.idDetalle == idDetalle).ToList();

                    foreach (var item in detalleE)
                    {
                        var ddObj = new DetallesVentaEspecial();

                        ddObj.idVenta = Idventa;
                        ddObj.idDetalle = idDetalle_;
                        ddObj.idProducto = item.idProducto;
                        ddObj.cantidad = item.cantidad;
                        ddObj.idCuadre = item.idCuadre;
                        ddObj.precioEntrada = item.precioEntrada;

                        ddL.Add(ddObj);
                    }

                    context.DetallesVentaEspecial.RemoveRange(detalleE);
                    context.DetallesVentaEspecial.AddRange(ddL);

                    context.SaveChanges();


                }

                DetalleVenta detalle = context.DetalleVenta.Find(idDetalle);
                context.Entry(detalle).State = System.Data.Entity.EntityState.Deleted;

                Venta venta = context.Venta.Find(detalle.idVenta);
                venta.total -= detalle.subTotal;

                context.SaveChanges();



            }
        }

        //Dividir cuentas
        [HttpPost]
        public JsonResult DividirReady(List<string> ListadoDividir)
        {
            try
            {



                using (var entity = new barbdEntities())
                {
                    int id = Convert.ToInt32(ListadoDividir[0]);
                    var objD = entity.DetalleVenta.Find(id);
                    var objV = entity.Venta.Find(objD.idVenta);
                    var objC = entity.Cliente.Find(objV.idCliente);
                    var M = entity.Cliente.Where(x => x.idMesa == null).First();

                    var objCliente = new Cliente()
                    {
                        nombre = objC.nombre + "D",
                        idMesa = M.idMesa

                    };
                    NuevaOrden(objCliente, Convert.ToInt32(objV.idUsuario), ListadoDividir);

                    var Info = new InfoMensaje
                    {
                        Tipo = "Ready",
                        Mensaje = "Cuenta Dividida con exito"
                    };

                    return Json(Info, JsonRequestBehavior.AllowGet);

                }
            }
            catch (Exception ex)
            {
                var Info = new InfoMensaje
                {
                    Tipo = "Error",
                    Mensaje = ex.Message
                };

                return Json(Info, JsonRequestBehavior.AllowGet);
            }

        }

        //Enlazar cuentas
        [HttpPost]
        public JsonResult EnlazarReady(List<string> ListadoEnlazar)
        {
            try
            {
                int CuentaOrigen = int.Parse(ListadoEnlazar[0]);

                using (var entity = new barbdEntities())
                {
                    for (int i = 1; i < ListadoEnlazar.Count; i++)
                    {
                        object[] xparams = {
                        new SqlParameter("@IdventaOrigen",  CuentaOrigen),
                        new SqlParameter("@IdVentaEnlazar",  int.Parse(ListadoEnlazar[i]))};

                        entity.Database.ExecuteSqlCommand("exec sp_EnlazarCuenta @IdventaOrigen, @IdVentaEnlazar", xparams);
                    }

                    var Info = new InfoMensaje
                    {
                        Tipo = "Ready",
                        Mensaje = "Cuenta Enlazada con exito"
                    };

                    return Json(Info, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                var Info = new InfoMensaje
                {
                    Tipo = "Error",
                    Mensaje = ex.Message
                };

                return Json(Info, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region Categoria
        //Inicio
        public ActionResult Categoria()
        {
            using (var entity = new barbdEntities())
            {
                ViewData["TipoCategoria"] = entity.TipoCategoria.ToList();
                ViewData["ListaCategoria"] = entity.Categoria.ToList();
            }
            return PartialView();
        }

        //Crear Categoria
        public ActionResult CrearCategoria(Categoria Objcategoria)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    entity.Categoria.Add(Objcategoria);
                    entity.SaveChanges();
                    ViewData["TipoCategoria"] = entity.TipoCategoria.ToList();
                    ViewData["ListaCategoria"] = entity.Categoria.ToList();
                    var Info = new InfoMensaje
                    {
                        Tipo = "Success",
                        Mensaje = "Categoria Agregada exitosamente"

                    };
                    ViewData["Alert"] = Info;

                    return PartialView("ListaCategoria");
                }
            }
            catch (Exception ex)
            {
                using (var entity1 = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message

                    };
                    ViewData["Alert"] = Info;
                    ViewData["TipoCategoria"] = entity1.TipoCategoria.ToList();
                    ViewData["ListaCategoria"] = entity1.Categoria.ToList();

                    return PartialView("ListaCategoria");
                }
            }
        }

        //Eliminar Categoria
        public ActionResult EliminarCategoria(int Id)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    var ValidarProducto = entity.Producto.Count(x => x.idCategoria == Id);

                    if (ValidarProducto == 0)
                    {
                        var ObjCategoria = entity.Categoria.Find(Id);
                        entity.Categoria.Remove(ObjCategoria);
                        entity.SaveChanges();
                        ViewData["TipoCategoria"] = entity.TipoCategoria.ToList();
                        ViewData["ListaCategoria"] = entity.Categoria.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Categoria Eliminada exitosamente"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaCategoria");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Categoria no puede ser eliminada, existen productos asignados"
                        };
                        ViewData["Alert"] = Info;
                        ViewData["TipoCategoria"] = entity.TipoCategoria.ToList();
                        ViewData["ListaCategoria"] = entity.Categoria.ToList();

                        return PartialView("ListaCategoria");
                    }
                }
            }
            catch (Exception ex)
            {
                using (var entity1 = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message
                    };
                    ViewData["Alert"] = Info;
                    ViewData["TipoCategoria"] = entity1.TipoCategoria.ToList();
                    ViewData["ListaCategoria"] = entity1.Categoria.ToList();

                    return PartialView("ListaCategoria");
                }
            }
        }

        //buscar para editar Categoria
        [HttpPost]
        public ActionResult BuscarEditarCategoria(Categoria categoria)
        {
            using (var entity = new barbdEntities())
            {
                var ObjCategoria = entity.Categoria.Find(categoria.idCategoria);

                var Ca = new Categoria
                {
                    idCategoria = ObjCategoria.idCategoria,
                    idTipoCategoria = ObjCategoria.idTipoCategoria,
                    nombre = ObjCategoria.nombre,
                    activo = ObjCategoria.activo


                };

                return Json(Ca, JsonRequestBehavior.AllowGet);
            }
        }

        //editar categoria
        [HttpPost]
        public ActionResult EditarCategoria(Categoria categoria)
        {
            try
            {
                string Activo = Request["activoE"].ToString();

                if (Activo == "false")
                {
                    categoria.activo = false;
                }
                else
                {
                    categoria.activo = true;
                }
                using (var entity = new barbdEntities())
                {
                    var ObjCategoria = entity.Categoria.Find(categoria.idCategoria);
                    ObjCategoria.idTipoCategoria = categoria.idTipoCategoria;
                    ObjCategoria.nombre = categoria.nombre;
                    ObjCategoria.activo = categoria.activo;
                    entity.SaveChanges();

                    var Info = new InfoMensaje
                    {
                        Tipo = "Success",
                        Mensaje = "Categoria Editada exitosamente"

                    };
                    ViewData["Alert"] = Info;
                    ViewData["TipoCategoria"] = entity.TipoCategoria.ToList();
                    ViewData["ListaCategoria"] = entity.Categoria.ToList();

                    return PartialView("ListaCategoria");
                }
            }
            catch (Exception ex)
            {
                using (var entity1 = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message

                    };
                    ViewData["Alert"] = Info;
                    ViewData["TipoCategoria"] = entity1.TipoCategoria.ToList();
                    ViewData["ListaCategoria"] = entity1.Categoria.ToList();

                    return PartialView("ListaCategoria");
                }
            }
        }
        #endregion

        #region Producto
        //Producto Inicio
        public ActionResult Producto()
        {
            using (var entity = new barbdEntities())
            {
                ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 1 || x.idTipoCategoria == 2).ToList();
                ViewData["ListaProducto"] = entity.Producto.Where(x => x.especial == false).ToList(); //entity.Producto.ToList();
            }

            return PartialView();
        }

        public ActionResult CrearProducto(Producto ObjProducto)
        {
            using (var entity = new barbdEntities())
            {
                try
                {

                    if (ModelState.IsValid)
                    {
                        ObjProducto.especial = false;
                        ObjProducto.precioFiesta = 0;
                        entity.Producto.Add(ObjProducto);
                        entity.SaveChanges();
                        ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 1 || x.idTipoCategoria == 2).ToList();
                        ViewData["ListaProducto"] = entity.Producto.Where(x => x.especial == false).ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Producto Agregado exitosamente"
                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaProducto");
                    }
                    else
                    {
                        ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 1 || x.idTipoCategoria == 2).ToList();
                        ViewData["ListaProducto"] = entity.Producto.Where(x => x.especial == false).ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Producto Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaProducto");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message
                        };
                        ViewData["Alert"] = Info;
                        ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 1 || x.idTipoCategoria == 2).ToList();
                        ViewData["ListaProducto"] = entity.Producto.Where(x => x.especial == false).ToList();

                        return PartialView("ListaProducto");
                    }
                }
            }
        }

        //Eliminar Producto
        [HttpPost]
        public ActionResult EliminarProducto(string Id)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    var ValidarProducto = entity.DetalleVenta.Count(x => x.idProducto == Id);

                    if (ValidarProducto == 0)
                    {
                        var ObjProducto = entity.Producto.Find(Id);
                        entity.Producto.Remove(ObjProducto);
                        entity.SaveChanges();
                        ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 1 || x.idTipoCategoria == 2).ToList();
                        ViewData["ListaProducto"] = entity.Producto.Where(x => x.especial == false).ToList();

                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Producto Eliminada exitosamente"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaProducto");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Producto no puede ser eliminada, existen ventas con producto"
                        };
                        ViewData["Alert"] = Info;
                        ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 1 || x.idTipoCategoria == 2).ToList();
                        ViewData["ListaProducto"] = entity.Producto.Where(x => x.especial == false).ToList();

                        return PartialView("ListaProducto");
                    }
                }
            }
            catch (Exception ex)
            {
                using (var entity = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message
                    };
                    ViewData["Alert"] = Info;
                    ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 1 || x.idTipoCategoria == 2).ToList();
                    ViewData["ListaProducto"] = entity.Producto.Where(x => x.especial == false).ToList();

                    return PartialView("ListaProducto");
                }
            }
        }

        //buscar para editar Producto
        [HttpPost]
        public ActionResult BuscarEditarProducto(Producto producto)
        {
            using (var entity = new barbdEntities())
            {
                var ObjProducto = entity.Producto.Find(producto.idProducto);

                var Ca = new Producto
                {
                    idProducto = ObjProducto.idProducto,
                    nombre = ObjProducto.nombre,
                    precioVenta = ObjProducto.precioVenta,
                    precioFiesta = ObjProducto.precioFiesta,
                    precioAlmacen = ObjProducto.precioAlmacen,
                    idCategoria = ObjProducto.idCategoria,
                    activo = ObjProducto.activo
                };

                return Json(Ca, JsonRequestBehavior.AllowGet);
            }
        }

        //eliminar producto
        [HttpPost]
        public ActionResult EditarProducto(Producto producto)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    string Activo = Request["activoE"].ToString();

                    if (Activo == "false")
                    {
                        producto.activo = false;
                    }
                    else
                    {
                        producto.activo = true;
                    }

                    if (!string.IsNullOrEmpty(producto.precioAlmacen.ToString()) && !string.IsNullOrEmpty(producto.precioVenta.ToString()) && !string.IsNullOrEmpty(producto.nombre.ToString()))
                    {
                        var ObjProducto = entity.Producto.Find(producto.idProducto);
                        ObjProducto.idCategoria = producto.idCategoria;
                        ObjProducto.nombre = producto.nombre;
                        ObjProducto.precioVenta = producto.precioVenta;
                        ObjProducto.precioFiesta = producto.precioFiesta;
                        ObjProducto.precioAlmacen = producto.precioAlmacen;
                        ObjProducto.activo = producto.activo;


                        entity.SaveChanges();

                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Producto Editada exitosamente"

                        };
                        ViewData["Alert"] = Info;
                        ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 1 || x.idTipoCategoria == 2).ToList();
                        ViewData["ListaProducto"] = entity.Producto.Where(x => x.especial == false).ToList();

                        return PartialView("ListaProducto");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Producto Tiene campos Invalidos"
                        };
                        ViewData["Alert"] = Info;
                        ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 1 || x.idTipoCategoria == 2).ToList();
                        ViewData["ListaProducto"] = entity.Producto.Where(x => x.especial == false).ToList();

                        return PartialView("ListaProducto");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message
                        };
                        ViewData["Alert"] = Info;
                        ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 1 || x.idTipoCategoria == 2).ToList();
                        ViewData["ListaProducto"] = entity.Producto.Where(x => x.especial == false).ToList();

                        return PartialView("ListaProducto");
                    }
                }
            }
        }
        #endregion

        #region ModoPago
        //Inicio
        public ActionResult ModoPago()
        {
            using (var entity = new barbdEntities())
            {
                ViewData["ListaModoPago"] = entity.ModoPago.Where(m => m.numPago > 0).ToList();
            }

            return PartialView();
        }

        //Crear Modo de pago
        public ActionResult CrearModoPago(ModoPago ObjModoPago)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        entity.ModoPago.Add(ObjModoPago);
                        entity.SaveChanges();
                        ViewData["ListaModoPago"] = entity.ModoPago.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Modo de Pago Agregado exitosamente"
                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaModoPago");
                    }
                    else
                    {
                        ViewData["ListaModoPago"] = entity.ModoPago.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Modo de Pago Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaModoPago");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaModoPago"] = entity.ModoPago.ToList();

                        return PartialView("ListaModoPago");
                    }
                }
            }
        }

        //Eliminar Modo de pago
        [HttpPost]
        public ActionResult EliminarModoPago(int Id)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    var ValidarProducto = entity.Factura.Count(x => x.numPago == Id);

                    if (ValidarProducto == 0)
                    {
                        var ObjModoPago = entity.ModoPago.Find(Id);
                        entity.ModoPago.Remove(ObjModoPago);
                        entity.SaveChanges();
                        ViewData["ListaModoPago"] = entity.ModoPago.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Modo de Pago Eliminada exitosamente"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaModoPago");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Modo de Pago no puede ser eliminada, existen ventas"
                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaModoPago"] = entity.ModoPago.ToList();

                        return PartialView("ListaModoPago");
                    }
                }
            }
            catch (Exception ex)
            {
                using (var entity = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message

                    };
                    ViewData["Alert"] = Info;
                    ViewData["ListaModoPago"] = entity.ModoPago.ToList();

                    return PartialView("ListaModoPago");
                }
            }
        }

        //buscar para editar Modo de pago
        [HttpPost]
        public ActionResult BuscarEditarModoPago(ModoPago modoPago)
        {
            using (var entity = new barbdEntities())
            {
                var ObjModoPago = entity.ModoPago.Find(modoPago.numPago);

                var Ca = new ModoPago
                {
                    numPago = ObjModoPago.numPago,
                    nombre = ObjModoPago.nombre,
                    otroDetalles = ObjModoPago.otroDetalles
                };

                return Json(Ca, JsonRequestBehavior.AllowGet);
            }
        }

        //eliminar producto

        [HttpPost]
        public ActionResult EditarModoPago(ModoPago modoPago)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        var ObjModoPago = entity.ModoPago.Find(modoPago.numPago);
                        ObjModoPago.nombre = modoPago.nombre;
                        ObjModoPago.otroDetalles = modoPago.otroDetalles;

                        entity.SaveChanges();

                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Modo de Pago Editada exitosamente"

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaModoPago"] = entity.ModoPago.ToList();

                        return PartialView("ListaModoPago");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Modo de Pago Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaModoPago"] = entity.ModoPago.ToList();

                        return PartialView("ListaModoPago");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaModoPago"] = entity.ModoPago.ToList();

                        return PartialView("ListaModoPago");
                    }
                }
            }
        }
        #endregion

        #region Suplidor
        //Inicio
        public ActionResult Suplidor()
        {
            using (var entity = new barbdEntities())
            {

                ViewData["ListaSuplidor"] = entity.Suplidor.ToList();
            }

            return PartialView();
        }

        //Crear suplidor
        public ActionResult CrearSuplidor(Suplidor ObjSuplidor)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        entity.Suplidor.Add(ObjSuplidor);
                        entity.SaveChanges();
                        ViewData["ListaSuplidor"] = entity.Suplidor.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Suplidor Agregado exitosamente"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaSuplidor");
                    }
                    else
                    {
                        ViewData["ListaSuplidor"] = entity.Suplidor.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Suplidor Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaSuplidor");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaSuplidor"] = entity.Suplidor.ToList();

                        return PartialView("ListaSuplidor");
                    }
                }
            }
        }

        //Eliminar suplidor
        [HttpPost]
        public ActionResult EliminarSuplidor(int Id)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    var ValidarSuplidor = entity.Inventario.Count(x => x.idSuplidor == Id);

                    if (ValidarSuplidor == 0)
                    {
                        var ObjSuplidor = entity.Suplidor.Find(Id);
                        entity.Suplidor.Remove(ObjSuplidor);
                        entity.SaveChanges();
                        ViewData["ListaSuplidor"] = entity.Suplidor.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Suplidor Eliminada exitosamente"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaSuplidor");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Suplidor no puede ser eliminada, existen compras"

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaSuplidor"] = entity.Suplidor.ToList();

                        return PartialView("ListaSuplidor");
                    }
                }
            }
            catch (Exception ex)
            {
                using (var entity = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message

                    };
                    ViewData["Alert"] = Info;
                    ViewData["ListaSuplidor"] = entity.Suplidor.ToList();

                    return PartialView("ListaSuplidor");
                }
            }
        }

        [HttpPost]
        public ActionResult BuscarEditarSuplidor(Suplidor suplidor)
        {
            using (var entity = new barbdEntities())
            {
                var ObjSuplidor = entity.Suplidor.Find(suplidor.idSuplidor);

                var Ca = new Suplidor
                {
                    idSuplidor = ObjSuplidor.idSuplidor,
                    nombre = ObjSuplidor.nombre,
                    rnc = ObjSuplidor.rnc,
                    telefono = ObjSuplidor.telefono,
                    correo = ObjSuplidor.correo,
                    direccion = ObjSuplidor.direccion
                };

                return Json(Ca, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult EditarSuplidor(Suplidor suplidor)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        var ObjSuplidor = entity.Suplidor.Find(suplidor.idSuplidor);
                        ObjSuplidor.idSuplidor = suplidor.idSuplidor;
                        ObjSuplidor.nombre = suplidor.nombre;
                        ObjSuplidor.rnc = suplidor.rnc;
                        ObjSuplidor.telefono = suplidor.telefono;
                        ObjSuplidor.correo = suplidor.correo;
                        ObjSuplidor.direccion = suplidor.direccion;

                        entity.SaveChanges();

                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Suplidor Editada exitosamente"

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaSuplidor"] = entity.Suplidor.ToList();

                        return PartialView("ListaSuplidor");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Suplidor Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaSuplidor"] = entity.Suplidor.ToList();

                        return PartialView("ListaSuplidor");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaSuplidor"] = entity.Suplidor.ToList();

                        return PartialView("ListaSuplidor");
                    }
                }
            }
        }
        #endregion

        #region Gastos
        public ActionResult Gastos()
        {
            using (var entity = new barbdEntities())
            {
                int idCuadre = entity.Cuadre.AsEnumerable().SingleOrDefault(c => !c.cerrado.GetValueOrDefault(false)).idCuadre;
                ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == idCuadre).ToList();
            }

            return PartialView();
        }

        public ActionResult CrearGastos(Gastos ObjGastos)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    ObjGastos.idCuadre = entity.Cuadre.AsEnumerable().SingleOrDefault(c => !c.cerrado.GetValueOrDefault(false)).idCuadre;

                    if (ModelState.IsValid)
                    {
                        entity.Gastos.Add(ObjGastos);
                        entity.SaveChanges();
                        ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == ObjGastos.idCuadre).ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Gasto Agregado exitosamente"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaGastos");
                    }
                    else
                    {
                        ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == ObjGastos.idCuadre).ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Gasto Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaGastos");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message

                        };
                        ViewData["Alert"] = Info;
                        int idCuadre = entity.Cuadre.AsEnumerable().SingleOrDefault(c => !c.cerrado.GetValueOrDefault(false)).idCuadre;
                        ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == idCuadre).ToList();

                        return PartialView("ListaGastos");
                    }
                }
            }
        }

        [HttpPost]
        public ActionResult EliminarGastos(int Id)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    var ObjGastos = entity.Gastos.Find(Id);
                    entity.Gastos.Remove(ObjGastos);
                    entity.SaveChanges();
                    int idCuadre = entity.Cuadre.AsEnumerable().SingleOrDefault(c => !c.cerrado.GetValueOrDefault(false)).idCuadre;
                    ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == idCuadre).ToList();
                    var Info = new InfoMensaje
                    {
                        Tipo = "Success",
                        Mensaje = "Gasto Eliminada exitosamente"

                    };
                    ViewData["Alert"] = Info;

                    return PartialView("ListaGastos");
                }
            }
            catch (Exception ex)
            {
                using (var entity = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message

                    };
                    ViewData["Alert"] = Info;
                    int idCuadre = entity.Cuadre.AsEnumerable().SingleOrDefault(c => !c.cerrado.GetValueOrDefault(false)).idCuadre;
                    ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == idCuadre).ToList();

                    return PartialView("ListaGastos");
                }
            }
        }

        [HttpPost]
        public ActionResult BuscarEditarGastos(Gastos gastos)
        {
            using (var entity = new barbdEntities())
            {
                var ObjGatos = entity.Gastos.Find(gastos.IdGastos);

                var Ca = new Gastos
                {
                    IdGastos = ObjGatos.IdGastos,
                    descripcion = ObjGatos.descripcion,
                    cantidad = ObjGatos.cantidad,
                    idCuadre = ObjGatos.idCuadre
                };

                return Json(Ca, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult EditarGastos(Gastos gastos)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        var ObjGastos = entity.Gastos.Find(gastos.IdGastos);
                        ObjGastos.descripcion = gastos.descripcion;
                        ObjGastos.cantidad = gastos.cantidad;

                        entity.SaveChanges();

                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Gasto Editada exitosamente"

                        };
                        ViewData["Alert"] = Info;
                        int idCuadre = entity.Cuadre.AsEnumerable().SingleOrDefault(c => !c.cerrado.GetValueOrDefault(false)).idCuadre;
                        ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == idCuadre).ToList();

                        return PartialView("ListaGastos");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Gasto Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;
                        int idCuadre = entity.Cuadre.AsEnumerable().SingleOrDefault(c => !c.cerrado.GetValueOrDefault(false)).idCuadre;
                        ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == idCuadre).ToList();

                        return PartialView("ListaGastos");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message

                        };
                        ViewData["Alert"] = Info;
                        int idCuadre = entity.Cuadre.AsEnumerable().SingleOrDefault(c => !c.cerrado.GetValueOrDefault(false)).idCuadre;
                        ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == idCuadre).ToList();

                        return PartialView("ListaGastos");
                    }
                }
            }
        }
        #endregion

        #region Usuario
        public ActionResult Usuario()
        {
            using (var entity = new barbdEntities())
            {

                ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                ViewData["TipoRolUsuario"] = entity.Roles.ToList();
            }

            return PartialView();
        }

        public ActionResult CrearUsuario(Usuario ObjUsuario)
        {

            //ObjUsuario.EnvioCorreo = !string.IsNullOrEmpty(ObjUsuario.EnvioCorreo.ToString())? true : false;
            //ObjUsuario.activo = !string.IsNullOrEmpty(ObjUsuario.activo.ToString()) ? true : false;
            //ObjUsuario.resetContrasena = !string.IsNullOrEmpty(ObjUsuario.resetContrasena.ToString()) ? true : false;


            // var a = FormCollection["EnvioCorreo"]

            using (var entity = new barbdEntities())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        if (ObjUsuario.contrasena.Length >= 6)
                        {
                            var ValidarContrasena = entity.Usuario.Where(x => x.contrasena == ObjUsuario.contrasena).Count();

                            if (ValidarContrasena == 0)
                            {
                                var ValidarNombre = entity.Usuario.Where(x => x.nombre == ObjUsuario.nombre).Count();

                                if (ValidarNombre == 0)
                                {
                                    entity.Usuario.Add(ObjUsuario);
                                    entity.SaveChanges();
                                    ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                                    ViewData["TipoRolUsuario"] = entity.Roles.ToList();
                                    var Info = new InfoMensaje
                                    {
                                        Tipo = "Success",
                                        Mensaje = "Usuario Agregado exitosamente"
                                    };
                                    ViewData["Alert"] = Info;

                                    return PartialView("ListaUsuario");
                                }
                                else
                                {
                                    ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                                    ViewData["TipoRolUsuario"] = entity.Roles.ToList();

                                    var Info2 = new InfoMensaje
                                    {
                                        Tipo = "Warning",
                                        Mensaje = "Nombre ya existe por otro usuario"

                                    };
                                    ViewData["Alert"] = Info2;
                                    return PartialView("ListaUsuario");

                                }
                            }
                            else
                            {
                                ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                                ViewData["TipoRolUsuario"] = entity.Roles.ToList();
                                var Info2 = new InfoMensaje
                                {
                                    Tipo = "Warning",
                                    Mensaje = "Contrasena ya existe por otro usuario"
                                };

                                ViewData["Alert"] = Info2;
                                return PartialView("ListaUsuario");
                            }
                        }
                        else
                        {
                            ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                            ViewData["TipoRolUsuario"] = entity.Roles.ToList();
                            var Info1 = new InfoMensaje
                            {
                                Tipo = "Warning",
                                Mensaje = "Contrasena debe ser  6 o mas Digitos"
                            };
                            ViewData["Alert"] = Info1;
                            return PartialView("ListaUsuario");
                        }
                    }
                    else
                    {
                        ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                        ViewData["TipoRolUsuario"] = entity.Roles.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Usuario Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaUsuario");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                        ViewData["TipoRolUsuario"] = entity.Roles.ToList();

                        return PartialView("ListaUsuario");
                    }
                }
            }
        }

        [HttpPost]
        public ActionResult EliminarUsuario(int Id)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    var Validar = entity.Venta.Count(x => x.idUsuario == Id);

                    if (Validar == 0)
                    {
                        var ObjUsuario = entity.Usuario.Find(Id);
                        entity.Usuario.Remove(ObjUsuario);
                        entity.SaveChanges();
                        ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                        ViewData["TipoRolUsuario"] = entity.Roles.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Usuario Eliminada exitosamente"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaUsuario");
                    }
                    else
                    {
                        ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                        ViewData["TipoRolUsuario"] = entity.Roles.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Usuario no se puede eliminar, tiene ventas realizadas"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaUsuario");
                    }
                }
            }
            catch (Exception ex)
            {
                using (var entity = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message

                    };
                    ViewData["Alert"] = Info;
                    ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                    ViewData["TipoRolUsuario"] = entity.Roles.ToList();

                    return PartialView("ListaUsuario");
                }
            }
        }

        [HttpPost]
        public ActionResult BuscarEditarUsuario(Usuario usuario)
        {
            using (var entity = new barbdEntities())
            {
                var ObjUsuario = entity.Usuario.Find(usuario.idUsuario);

                var Ca = new Usuario
                {
                    idUsuario = ObjUsuario.idUsuario,
                    nombre = ObjUsuario.nombre,
                    contrasena = ObjUsuario.contrasena,
                    idTarjeta = ObjUsuario.idTarjeta,
                    Correo = ObjUsuario.Correo,
                    idRol = ObjUsuario.idRol,
                    activo = ObjUsuario.activo,
                    resetContrasena = ObjUsuario.resetContrasena,
                    EnvioCorreo = ObjUsuario.EnvioCorreo
                };

                return Json(Ca, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult EditarUsuario(Usuario usuario, FormCollection formCollection)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    usuario.EnvioCorreo = !string.IsNullOrEmpty(usuario.EnvioCorreo.ToString()) ? true : false;
                    usuario.activo = !string.IsNullOrEmpty(usuario.activo.ToString()) ? true : false;
                    usuario.resetContrasena = !string.IsNullOrEmpty(usuario.resetContrasena.ToString()) ? true : false;

                    if (ModelState.IsValid)
                    {
                        if (usuario.contrasena.Length >= 6)
                        {
                            string Activo = formCollection.Get("activoUsuarioE").ToString();
                            string resetContrasena = Request["resetContrasenaUsuarioE"].ToString();
                            string EnvioCorreo = Request["EnvioCorreoUsuarioE"].ToString();
                            var ObjUsuario = entity.Usuario.Find(usuario.idUsuario);

                            ObjUsuario.nombre = usuario.nombre;
                            ObjUsuario.contrasena = usuario.contrasena;
                            ObjUsuario.idTarjeta = usuario.idTarjeta;
                            ObjUsuario.Correo = usuario.Correo;
                            ObjUsuario.idRol = usuario.idRol;

                            if (Activo == "false")
                            {
                                ObjUsuario.activo = false;
                            }
                            else
                            {
                                ObjUsuario.activo = true;
                            }

                            if (resetContrasena == "false")
                            {
                                ObjUsuario.resetContrasena = false;
                            }
                            else
                            {
                                ObjUsuario.resetContrasena = true;
                            }

                            if (EnvioCorreo == "false")
                            {
                                ObjUsuario.EnvioCorreo = false;
                            }
                            else
                            {
                                ObjUsuario.EnvioCorreo = true;
                            }

                            entity.SaveChanges();

                            var Info = new InfoMensaje
                            {
                                Tipo = "Success",
                                Mensaje = "Usuario Editada exitosamente"

                            };
                            ViewData["Alert"] = Info;
                            ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                            ViewData["TipoRolUsuario"] = entity.Roles.ToList();

                            return PartialView("ListaUsuario");
                        }
                        else
                        {
                            var Info1 = new InfoMensaje
                            {
                                Tipo = "Warning",
                                Mensaje = "Contrasena debe ser 6 o mas Digitos"

                            };
                            ViewData["Alert"] = Info1;
                            ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                            ViewData["TipoRolUsuario"] = entity.Roles.ToList();

                            return PartialView("ListaUsuario");
                        }
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Usuario Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                        ViewData["TipoRolUsuario"] = entity.Roles.ToList();

                        return PartialView("ListaUsuario");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                        ViewData["TipoRolUsuario"] = entity.Roles.ToList();

                        return PartialView("ListaUsuario");
                    }
                }
            }
        }
        #endregion

        #region Inventario
        public ActionResult Inventario()
        {
            using (var entity = new barbdEntities())
            {
                ViewData["TipoCategoria"] = TipoCategoria;
                ViewData["TransferirVista"] = "Si";
                var queryInventario = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleInventario");
                var queryProducto = entity.Producto.Where(x => x.Categoria.idTipoCategoria != 3 && x.activo == true).ToList().Select(x => new { x.idProducto, x.nombre });
                var querySuplidor = entity.Suplidor.ToList().Select(x => new { x.idSuplidor, x.nombre });

                ViewData["ListaInventario"] = queryInventario.ToList();
                ViewData["Producto"] = queryProducto.ToList();
                ViewData["Suplidor"] = querySuplidor.ToList();

            }

            return PartialView();
        }

        public ActionResult CrearEntrdaInventario(Inventario inventario)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    ViewData["TipoCategoria"] = TipoCategoria;
                    if (ModelState.IsValid)
                    {
                        ViewData["TransferirVista"] = "Si";
                        inventario.fecha = DateTime.Now;
                        inventario.idUsuario = Convert.ToInt32(Session["IdUsuario"]);
                        entity.Inventario.Add(inventario);
                        entity.SaveChanges();

                        var Info = new InfoMensaje
                        {
                            Tipo = "Success1",
                            Mensaje = "Producto Agregado en almacen"

                        };
                        ViewData["Alert"] = Info;
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Producto tiene campo Invalido"

                        };
                        ViewData["Alert"] = Info;
                    }

                    var queryInventario = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleInventario");

                    ViewData["ListaInventario"] = queryInventario.ToList();
                }

                return PartialView("ListaInventario");
            }
            catch (Exception ex)
            {
                var Info = new InfoMensaje
                {
                    Tipo = "Warning",
                    Mensaje = ex.Message

                };
                ViewData["Alert"] = Info;
                using (var entity = new barbdEntities())
                {
                    var queryInventario = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleInventario");

                    ViewData["ListaInventario"] = queryInventario.ToList();
                }

                return PartialView("ListaInventario");
            }
        }



        public ActionResult InventarioAlmacen()
        {
            ViewData["TipoCategoria"] = TipoCategoria;
            using (var entity = new barbdEntities())
            {
                ViewData["TransferirVista"] = "Si";
                var queryInventario = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleInventario");

                ViewData["ListaInventario"] = queryInventario.ToList();
            }

            return PartialView("ListaInventario", ViewData["ListaInventario"]);
        }

        public ActionResult InventarioBar()
        {
            ViewData["TipoCategoria"] = TipoCategoria;
            using (var entity = new barbdEntities())
            {
                ViewData["TransferirVista"] = null;
                var queryInventarioBAR = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleBAR");

                ViewData["ListaInventario"] = queryInventarioBAR.ToList();
            }

            return PartialView("ListaInventario", ViewData["ListaInventario"]);
        }

        [HttpPost]
        public ActionResult BuscarTransferirInventario(Inventario inventario)
        {
            ViewData["TipoCategoria"] = TipoCategoria;
            using (var entity = new barbdEntities())
            {
                var ObjInventarioProducto = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleInventario");
                var QueryProducto = ObjInventarioProducto.Single(x => x.IdProducto == inventario.idProducto);
                var Ca = new Inventario
                {
                    idProducto = QueryProducto.IdProducto,
                    cantidad = QueryProducto.Cantidad
                };

                return Json(Ca, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult TransferirProductoABar(InventarioBar inventarioBar)
        {
            try
            {
                ViewData["TipoCategoria"] = TipoCategoria;
                if (inventarioBar.cantidad > 0)
                {
                    using (var entity = new barbdEntities())
                    {
                        ViewData["TransferirVista"] = "Si";
                        inventarioBar.fecha = DateTime.Now;
                        inventarioBar.idUsuario = Convert.ToInt32(Session["IdUsuario"]);
                        entity.InventarioBar.Add(inventarioBar);
                        entity.SaveChanges();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Cantidad de producto transferito exitosamente"
                        };

                        ViewData["Alert"] = Info;
                        var queryInventario = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleInventario");
                        ViewData["ListaInventario"] = queryInventario.ToList();

                        return PartialView("ListaInventario", ViewData["ListaInventario"]);
                    }
                }
                else
                {
                    ViewData["TransferirVista"] = "Si";
                    using (var entity = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Transferencia de producto tiene campos invalidos"
                        };

                        ViewData["Alert"] = Info;
                        var queryInventario = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleInventario");
                        ViewData["ListaInventario"] = queryInventario.ToList();

                        return PartialView("ListaInventario", ViewData["ListaInventario"]);
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["TransferirVista"] = "Si";

                using (var entity = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message
                    };
                    var queryInventarioBAR = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleInventario");
                    ViewData["ListaInventario"] = queryInventarioBAR.ToList();

                    return PartialView("ListaInventario", ViewData["ListaInventario"]);
                }
            }
        }
        [HttpPost]
        public ActionResult ImprimirEntradaAlmacen()
        {
            try
            {
                using (barbdEntities context = new barbdEntities())
                {

                    Printer printer = new Printer();

                    printer.AddTitle("Entrada de Producto (Almacen)");
                    printer.AddSpace(2);
                    printer.AddSubtitle("Fecha: " + DateTime.Now.ToLongDateString());
                    printer.AddSpace(1);


                    string[][] resumen =
                        context.Database.SqlQuery<Models.CuadreTable>("exec sp_ImprirEntradaAlmacer")
                        .Select(r =>
                            new string[3]
                            {
                                r.Nombre,
                                r.Cantidad.ToString(),
                                r.Costo.ToString("$#,0.00"),
                            }
                        ).ToArray();

                    printer.AddTable(new string[3] { "Producto", "Cantidad", "Costo" }, resumen);

                    if (resumen.Count() > 0)
                    {
                        printer.Print();
                        return Json("Si", JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json("no", JsonRequestBehavior.AllowGet);
                    }


                }
            }
            catch (Exception ex)
            {

                return Json(ex, JsonRequestBehavior.AllowGet);
            }

        }

        #endregion

        #region Cobrar
        public ActionResult Cobrar()
        {
            using (barbdEntities entity = new barbdEntities())
            {
                ViewData["AllCreditos"] = entity.Creditos.Include("Pagos").AsEnumerable().Select(c => UntrackedCredito(c)).ToArray();
                ViewData["ListaCreditos"] = entity.Creditos.Include("Usuario").Include("Pagos").Where(c => c.MontoRestante > 0).ToArray();
                ViewData["ListaHistoricoCreditos"] = entity.Creditos.Include("Usuario").Include("Pagos").Where(c => c.MontoRestante == 0).ToArray();
            }

            return PartialView();
        }

        [HttpPost]
        public ActionResult PagarCredito(int id, decimal monto)
        {
            try
            {
                using (barbdEntities entity = new barbdEntities())
                {
                    InfoMensaje Info;
                    Creditos credito = entity.Creditos.Find(id);

                    if (monto <= credito.MontoRestante)
                    {
                        int idCuadre = entity.Cuadre.Where(x => x.cerrado == false).Select(x => x.idCuadre).FirstOrDefault();
                        Pagos pago = new Pagos()
                        {
                            idCredito = id,
                            Monto = monto,
                            Fecha = DateTime.Now,
                            idCuadre = idCuadre
                        };

                        entity.Pagos.Add(pago);
                        credito.MontoRestante -= monto;

                        entity.SaveChanges();

                        Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Pago realizado exitosamente"

                        };
                    }
                    else
                    {
                        Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "El monto a pagar debe ser menor que el monto en deuda"

                        };
                    }

                    ViewData["Alert"] = Info;

                }
                using (barbdEntities entity = new barbdEntities())
                {


                    ViewData["AllCreditos"] = entity.Creditos.Include("Pagos").AsEnumerable().Select(c => UntrackedCredito(c)).ToArray();
                    ViewData["ListaCreditos"] = entity.Creditos.Include("Usuario").Include("Pagos").Where(c => c.MontoRestante > 0).ToArray();
                    ViewData["ListaHistoricoCreditos"] = entity.Creditos.Include("Usuario").Include("Pagos").Where(c => c.MontoRestante == 0).ToArray();

                    return PartialView("ListaCreditos", ViewData["ListaCreditos"]);

                }

            }
            catch (Exception ex)
            {
                using (barbdEntities entity = new barbdEntities())
                {
                    InfoMensaje Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message

                    };

                    ViewData["Alert"] = Info;
                    ViewData["AllCreditos"] = entity.Creditos.Include("Pagos").AsEnumerable().Select(c => UntrackedCredito(c)).ToArray();
                    ViewData["ListaCreditos"] = entity.Creditos.Include("Usuario").Include("Pagos").Where(c => c.MontoRestante > 0).ToArray();
                    ViewData["ListaHistoricoCreditos"] = entity.Creditos.Include("Usuario").Include("Pagos").Where(c => c.MontoRestante == 0).ToArray();

                    return PartialView("ListaCreditos");
                }
            }
        }

        private Creditos UntrackedCredito(Creditos credito)
        {
            List<Pagos> pagos = new List<Pagos>();

            foreach (Pagos pago in credito.Pagos)
            {
                pagos.Add(new Pagos()
                {
                    id = pago.id,
                    Monto = pago.Monto,
                    Fecha = pago.Fecha
                });
            }

            return new Creditos()
            {
                id = credito.id,
                idUsuario = credito.idUsuario,
                MontoRestante = credito.MontoRestante,
                numFactura = credito.numFactura,
                Pagos = pagos.ToArray()
            };
        }
        #endregion

        #region Especiales
        public ActionResult Especiales()
        {
            using (var entity = new barbdEntities())
            {
                ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 3).ToList();
                ViewData["TipoProducto"] = entity.Producto.Where(x => x.especial == false).Select(p => new { p.idProducto, p.nombre }).ToList();
                ViewData["ListaEspeciales"] = entity.Producto.Where(x => x.especial == true).ToList();
            }

            return PartialView();
        }


        public ActionResult CrearEspeciales(Producto ObjProducto)
        {
            using (var entity = new barbdEntities())
            {
                try
                {

                    if (ModelState.IsValid)
                    {
                        ObjProducto.especial = true;
                        entity.Producto.Add(ObjProducto);
                        entity.SaveChanges();
                        ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 3).ToList();
                        ViewData["TipoProducto"] = entity.Producto.Where(x => x.especial == false).Select(p => new { p.idProducto, p.nombre }).ToList();
                        ViewData["ListaEspeciales"] = entity.Producto.Where(x => x.especial == true).ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Especial Agregado exitosamente"
                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaEspeciales");
                    }
                    else
                    {
                        ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 3).ToList();
                        ViewData["TipoProducto"] = entity.Producto.Where(x => x.especial == false).Select(p => new { p.idProducto, p.nombre }).ToList();
                        ViewData["ListaEspeciales"] = entity.Producto.Where(x => x.especial == true).ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Especial Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaEspeciales");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message
                        };
                        ViewData["Alert"] = Info;
                        ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 3).ToList();
                        ViewData["TipoProducto"] = entity.Producto.Where(x => x.especial == false).Select(p => new { p.idProducto, p.nombre }).ToList();
                        ViewData["ListaEspeciales"] = entity.Producto.Where(x => x.especial == true).ToList();
                        return PartialView("ListaEspeciales");
                    }
                }
            }
        }

        [HttpPost]
        public ActionResult BuscarEditarEspecial(Producto producto)
        {
            using (var entity = new barbdEntities())
            {
                var ObjProducto = entity.Producto.Find(producto.idProducto);

                var Ca = new Producto
                {
                    idProducto = ObjProducto.idProducto,
                    nombre = ObjProducto.nombre,
                    precioVenta = ObjProducto.precioVenta,
                    iniciolEspecial = ObjProducto.iniciolEspecial,
                    finalEspecial = ObjProducto.finalEspecial,
                    activo = ObjProducto.activo,
                    idCategoria = ObjProducto.idCategoria
                };

                return Json(Ca, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult EditarEspeciales(Producto producto)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    string Activo = Request["activoF"].ToString();

                    if (Activo == "false")
                    {
                        producto.activo = false;
                    }
                    else
                    {
                        producto.activo = true;
                    }

                    if (!string.IsNullOrEmpty(producto.precioVenta.ToString()) && !string.IsNullOrEmpty(producto.nombre.ToString()))
                    {
                        var ObjProducto = entity.Producto.Find(producto.idProducto);
                        ObjProducto.idCategoria = producto.idCategoria;
                        ObjProducto.nombre = producto.nombre;
                        ObjProducto.precioVenta = producto.precioVenta;
                        ObjProducto.iniciolEspecial = producto.iniciolEspecial;
                        ObjProducto.finalEspecial = producto.finalEspecial;
                        ObjProducto.activo = producto.activo;


                        entity.SaveChanges();

                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Producto Editada exitosamente"

                        };
                        ViewData["Alert"] = Info;
                        ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 3).ToList();
                        ViewData["TipoProducto"] = entity.Producto.Where(x => x.especial == false).Select(p => new { p.idProducto, p.nombre }).ToList();
                        ViewData["ListaEspeciales"] = entity.Producto.Where(x => x.especial == true).ToList();

                        return PartialView("ListaEspeciales");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Producto Tiene campos Invalidos"
                        };
                        ViewData["Alert"] = Info;
                        ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 3).ToList();
                        ViewData["TipoProducto"] = entity.Producto.Where(x => x.especial == false).Select(p => new { p.idProducto, p.nombre }).ToList();
                        ViewData["ListaEspeciales"] = entity.Producto.Where(x => x.especial == true).ToList();


                        return PartialView("ListaEspeciales");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message
                        };
                        ViewData["Alert"] = Info;
                        ViewData["Alert"] = Info;
                        ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 3).ToList();
                        ViewData["TipoProducto"] = entity.Producto.Where(x => x.especial == false).Select(p => new { p.idProducto, p.nombre }).ToList();
                        ViewData["ListaEspeciales"] = entity.Producto.Where(x => x.especial == true).ToList();


                        return PartialView("ListaEspeciales");
                    }
                }
            }
        }

        [HttpPost]
        public ActionResult EliminarEspeciales(string Id)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    var ValidarProducto = entity.DetalleVenta.Count(x => x.idProducto == Id);

                    if (ValidarProducto == 0)
                    {
                        var ObjProducto = entity.Producto.Find(Id);
                        entity.Producto.Remove(ObjProducto);
                        entity.SaveChanges();
                        ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 3).ToList();
                        ViewData["TipoProducto"] = entity.Producto.Where(x => x.especial == false).Select(p => new { p.idProducto, p.nombre }).ToList();
                        ViewData["ListaEspeciales"] = entity.Producto.Where(x => x.especial == true).ToList();


                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Especial Eliminada exitosamente"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaEspeciales");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Especial no puede ser eliminada, existen ventas con producto"
                        };
                        ViewData["Alert"] = Info;
                        ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 3).ToList();
                        ViewData["TipoProducto"] = entity.Producto.Where(x => x.especial == false).Select(p => new { p.idProducto, p.nombre }).ToList();
                        ViewData["ListaEspeciales"] = entity.Producto.Where(x => x.especial == true).ToList();


                        return PartialView("ListaEspeciales");
                    }
                }
            }
            catch (Exception ex)
            {
                using (var entity = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message
                    };
                    ViewData["Alert"] = Info;
                    ViewData["TipoCategoria"] = entity.Categoria.Where(x => x.idTipoCategoria == 3).ToList();
                    ViewData["TipoProducto"] = entity.Producto.Where(x => x.especial == false).Select(p => new { p.idProducto, p.nombre }).ToList();
                    ViewData["ListaEspeciales"] = entity.Producto.Where(x => x.especial == true).ToList();

                    return PartialView("ListaEspeciales");
                }
            }
        }

        [HttpPost]
        public ActionResult EliminarCombo(Producto Objproducto)
        {
            List<Producto> listp = new List<Producto>();

            using (var entity = new barbdEntities())
            {
                var p = entity.Producto.Find(Objproducto.idProducto);

                p.idProductoEspecial = "";
                p.idProductoEspecialCantidad = "";
                entity.SaveChanges();
                return PartialView("ListaProductoEspecial", listp);
            }

        }

        [HttpPost]
        public ActionResult CrearProductoEspeciales(Producto Objproducto)
        {

            try
            {
                using (var entity1 = new barbdEntities())
                {

                    var P = entity1.Producto.Find(Objproducto.idProducto);

                    P.idProductoEspecialCantidad += string.IsNullOrEmpty(P.idProductoEspecialCantidad) ? Objproducto.idProductoEspecialCantidad : "," + Objproducto.idProductoEspecialCantidad;

                    P.idProductoEspecial += string.IsNullOrEmpty(P.idProductoEspecial) ? Objproducto.idProductoEspecial.ToString() : "," + Objproducto.idProductoEspecial.ToString();

                    entity1.SaveChanges();

                    List<Producto> ListP = new List<Producto>();

                    using (var entity = new barbdEntities())
                    {

                        var P1 = entity.Producto.Find(Objproducto.idProducto);

                        if (!string.IsNullOrEmpty(P1.idProductoEspecial))
                        {
                            string[] ipe = P1.idProductoEspecial.Split(',');
                            string[] ipec = P1.idProductoEspecialCantidad.Split(',');

                            int contador = 0;

                            foreach (var producto in ipe)
                            {
                                var Oproducto = new Producto();

                                var p = entity.Producto.Find(ipe[contador]);

                                Oproducto.nombre = p.nombre;

                                foreach (var cantidad in ipec)
                                {

                                    Oproducto.idProductoEspecialCantidad = ipec[contador];

                                }

                                ListP.Add(Oproducto);
                                contador += 1;

                            }
                            return PartialView("ListaProductoEspecial", ListP);

                        }
                        else
                        {
                            return PartialView("ListaProductoEspecial", ListP);
                        }



                    }

                }
            }
            catch (Exception ex)
            {
                var Info = new InfoMensaje
                {
                    Tipo = "Error",
                    Mensaje = ex.Message
                };
                ViewData["Alert"] = Info;
                return Json(Info, JsonRequestBehavior.AllowGet);
            }

        }

        [HttpPost]
        public ActionResult BuscarProductoEspecial(Producto Objproducto)
        {



            List<Producto> ListP = new List<Producto>();

            using (var entity = new barbdEntities())
            {

                var P = entity.Producto.Find(Objproducto.idProducto);

                if (!string.IsNullOrEmpty(P.idProductoEspecial))
                {
                    string[] ipe = P.idProductoEspecial.Split(',');
                    string[] ipec = P.idProductoEspecialCantidad.Split(',');

                    int contador = 0;

                    foreach (var producto in ipe)
                    {

                        var Oproducto = new Producto();

                        var p = entity.Producto.Find(ipe[contador]);

                        Oproducto.nombre = p.nombre;

                        foreach (var cantidad in ipec)
                        {

                            Oproducto.idProductoEspecialCantidad = ipec[contador];

                        }

                        ListP.Add(Oproducto);
                        contador += 1;

                    }
                    return PartialView("ListaProductoEspecial", ListP);

                }
                else
                {
                    return PartialView("Listap", ListP);
                }








            }

        }

        #endregion

        #region Reportes

        public ActionResult Reportes()
        {
            using (barbdEntities entity = new barbdEntities())
            {
                //ViewData["AllCreditos"] = entity.Creditos.Include("Pagos").AsEnumerable().Select(c => UntrackedCredito(c)).ToArray();
                //ViewData["ListaCreditos"] = entity.Creditos.Include("Usuario").Include("Pagos").Where(c => c.MontoRestante > 0).ToArray();
                //ViewData["ListaHistoricoCreditos"] = entity.Creditos.Include("Usuario").Include("Pagos").Where(c => c.MontoRestante == 0).ToArray();
                List<Venta> l = new List<Venta>();
                List<DetalleVenta> d = new List<DetalleVenta>();
                List<Inventario> i = new List<Inventario>();
                List<Gastos> g = new List<Gastos>();

                var mp = entity.ModoPago.Select(x => new { x.numPago, x.nombre }).ToList();

                List<ModoPago> listModoPago = new List<ModoPago>();

                ModoPago objm1 = new ModoPago
                {
                    numPago = -2,
                    nombre = "Todo"

                };

                listModoPago.Add(objm1);

                foreach (var item in mp)
                {
                    ModoPago objm = new ModoPago
                    {
                        numPago = item.numPago,
                        nombre = item.nombre

                    };

                    listModoPago.Add(objm);

                }

                List<Localidad> listLocalidad = new List<Localidad>();

                Localidad localidad = new Localidad
                {
                    Id = 1,
                    Nombre = "Almacen"

                };
                listLocalidad.Add(localidad);

                Localidad localidad1 = new Localidad
                {
                    Id = 2,
                    Nombre = "Bar/Hookah/Restaurante/" +TipoCategoria

                };
                listLocalidad.Add(localidad1);


                var ModoPago_ = listModoPago.Select(x => new { x.numPago, x.nombre }).ToList();

                ViewData["ListadoGastos"] = g.ToList();

                ViewData["TipoLocalidad"] = listLocalidad;

                ViewData["TipoPago"] = ModoPago_;

                ViewData["ListadoDetallesOrdenes_"] = d.ToList();

                ViewData["ListadoInventario"] = i.ToList();


                return PartialView(l.ToArray());
            }


        }

        [HttpPost]
        public ActionResult IndexDetalles(string Id)
        {
            using (var entity = new barbdEntities())
            {
                int idVenta = Convert.ToInt32(Id);
                var ListaDetalleVenta = entity.DetalleVenta.Include("Venta").Include("Producto").Where(x => x.idVenta == idVenta).ToList();

                return PartialView("ListadoDetallesOrdenes", ListaDetalleVenta);
            }
        }


        public ActionResult ReportesVentasTotales(RangoFecha rango)
        {
            // return null;
            try
            {


                using (barbdEntities entity = new barbdEntities())
                {


                    List<Venta> v = new List<Venta>();


                    var idcuadre = entity.Cuadre.Where(x => x.fecha >= rango.fechaInicio && x.fecha <= rango.fechaFinal).Select(x => x.idCuadre).ToList();

                    foreach (var item in idcuadre)
                    {
                        Venta objv = new Venta();
                        objv.idCuadre = item;

                        v.Add(objv);
                    }

                    var t = v.Select(x => x.idCuadre).ToList();


                    if (rango.index == -2)
                    {



                        var ReportesVentas_ = entity.Venta.Include("cuadre").Include("factura").Include("usuario").Include("detalleventa").Include("DetallesVentaEspecial").Where(x => t.Contains(x.idCuadre) && x.ordenFacturada == true).ToList();

                        var ReportesVentas_1 = entity.Venta.Include("cuadre").Include("factura").Include("usuario").Include("detalleventa").Include("DetallesVentaEspecial").Where(x => t.Contains(x.idCuadre) && x.ordenFacturada == true).Select(x => x.idVenta).ToList();


                        List<Factura> vv = new List<Factura>();

                        foreach (var item in ReportesVentas_1)
                        {
                            Factura objv = new Factura();
                            objv.idVenta = item;

                            vv.Add(objv);
                        }

                        var tt = vv.Select(x => x.idVenta).ToList();


                        float ganacina = 0;


                        if (tt.Count > 0)
                        {
                            ganacina = entity.Factura.Where(x => tt.Contains(x.idVenta)).Sum(x => x.total);
                        }



                        decimal contador = 0;

                        foreach (var item in ReportesVentas_)
                        {

                            foreach (var item1 in item.DetalleVenta)
                            {

                                //decimal precioentrada = entity.Factura.Where(x => x.idVenta == item1.idVenta).SingleOrDefault().total;

                                contador += item1.cantidad * Convert.ToDecimal(item1.precioEntrada);

                            }

                            foreach (var item2 in item.DetallesVentaEspecial)
                            {

                                contador += Convert.ToDecimal(item2.cantidad) * Convert.ToDecimal(item2.precioEntrada);
                            }

                        }

                        VentasTotales vt = new VentasTotales
                        {
                            TotalVenta = ganacina,
                            TotalGanancias = Convert.ToDecimal(ganacina) - contador //TotalGanancias = ReportesVentas_.Sum(x => x.DetalleVenta.Single().precioVenta); //- ReportesVentas_.Sum(x => x.fa),


                        };

                        return Json(vt, JsonRequestBehavior.AllowGet);



                    }
                    else
                    {
                        var ReportesVentas_ = entity.Venta.Include("cuadre").Include("factura").Include("usuario").Include("detalleventa").Include("DetallesVentaEspecial").Where(x => t.Contains(x.idCuadre) && x.ordenFacturada == true && x.Factura.FirstOrDefault().numPago == rango.index).ToList();

                        var ReportesVentas_1 = entity.Venta.Include("cuadre").Include("factura").Include("usuario").Include("detalleventa").Include("DetallesVentaEspecial").Where(x => t.Contains(x.idCuadre) && x.ordenFacturada == true && x.Factura.FirstOrDefault().numPago == rango.index).Select(x => x.idVenta).ToList();


                        List<Factura> vv = new List<Factura>();

                        foreach (var item in ReportesVentas_1)
                        {
                            Factura objv = new Factura();
                            objv.idVenta = item;

                            vv.Add(objv);
                        }

                        var tt = vv.Select(x => x.idVenta).ToList();


                        float ganacina = 0;


                        if (tt.Count > 0)
                        {
                            ganacina = entity.Factura.Where(x => tt.Contains(x.idVenta)).Sum(x => x.total);
                        }



                        decimal contador = 0;

                        foreach (var item in ReportesVentas_)
                        {

                            foreach (var item1 in item.DetalleVenta)
                            {
                                contador += item1.cantidad * Convert.ToDecimal(item1.precioEntrada);

                            }

                            foreach (var item2 in item.DetallesVentaEspecial)
                            {

                                contador += Convert.ToDecimal(item2.cantidad) * Convert.ToDecimal(item2.precioEntrada);
                            }


                        }

                        VentasTotales vt = new VentasTotales
                        {
                            TotalVenta = ganacina,
                            TotalGanancias = Convert.ToDecimal(ganacina) - contador //TotalGanancias = ReportesVentas_.Sum(x => x.DetalleVenta.Single().precioVenta); //- ReportesVentas_.Sum(x => x.fa),


                        };



                        return Json(vt, JsonRequestBehavior.AllowGet);
                        //  return PartialView("ListaReportesVentas", ReportesVentas_);
                    }


                }
            }
            catch (Exception)
            {

                throw;
            }




        }


        [HttpPost]
        public ActionResult ReportesGastos(RangoFecha rango)
        {
            try
            {

                using (barbdEntities entity = new barbdEntities())
                {

                    List<Gastos> v1 = new List<Gastos>();

                    var v = entity.Gastos.ToList();

                    var idcuadre = entity.Cuadre.Where(x => x.fecha >= rango.fechaInicio && x.fecha <= rango.fechaFinal).Select(x => x.idCuadre).ToList();

                    foreach (var item in idcuadre)
                    {
                        Gastos objv = new Gastos();
                        objv.idCuadre = item;

                        v1.Add(objv);
                    }

                    var t1 = v1.Select(x => x.idCuadre).ToList();

                    var t = v.Where(x => t1.Contains(x.idCuadre));


                    return PartialView("ListadoReportesGasto", t);

                }

            }
            catch (Exception ex)
            {

                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }


        }


        [HttpPost]
        public ActionResult ReportesInventario(RangoFecha rango)
        {
            try
            {
                DateTime f = rango.fechaFinal.AddHours(23).AddMinutes(59);


                using (barbdEntities entity = new barbdEntities())
                {

                    if (rango.index == 1)
                    {
                        var InventarioAlmacen = entity.Inventario.Include("Producto").Include("Usuario").Where(x => x.fecha >= rango.fechaInicio && x.fecha <= f);
                        ViewData["ListadoInventario"] = InventarioAlmacen.ToList();
                        return PartialView("ListaReportesInventario");
                    }
                    else
                    {
                        var InventarioBar = entity.InventarioBar.Include("Producto").Include("Usuario").Where(x => x.fecha >= rango.fechaInicio && x.fecha <= f).ToList();
                        ViewData["ListadoInventarioBar"] = InventarioBar.ToList();
                        return PartialView("ListaReportesInventario");

                    }



                }

            }
            catch (Exception ex)
            {

                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }


        }


        [HttpPost]
        public ActionResult ReportesVentas(RangoFecha rango)
        {

            try
            {


                using (barbdEntities entity = new barbdEntities())
                {
                    List<Venta> v = new List<Venta>();

                    var idcuadre = entity.Cuadre.Where(x => x.fecha >= rango.fechaInicio && x.fecha <= rango.fechaFinal).Select(x => x.idCuadre).ToList();

                    foreach (var item in idcuadre)
                    {
                        Venta objv = new Venta();
                        objv.idCuadre = item;

                        v.Add(objv);
                    }

                    var t = v.Select(x => x.idCuadre).ToList();

                    //var ReportesVentas_ = (from venta in entity.Venta
                    //                       join cuadre in entity.Cuadre on venta.idCuadre equals cuadre.idCuadre
                    //                       join usuario in entity.Usuario on venta.idUsuario equals usuario.idUsuario
                    //                       join detalleventa in entity.DetalleVenta
                    //                       on venta.idVenta equals detalleventa.idVenta
                    //                       join factura in entity.Factura on venta.idVenta equals factura.idVenta
                    //                       join tipago in entity.ModoPago on factura.numPago equals tipago.numPago  
                    //                       //from tt in t where t.Contains(venta.idCuadre)
                    //                       where venta.ordenFacturada == true
                    //                       select new  {venta.idVenta, usuario.nombre, cuadre.idCuadre,tipago.nombre }).ToList();


                    if (rango.index == -2)
                    {
                        var ReportesVentas_ = entity.Venta.Include("cuadre").Include("factura").Include("usuario").Include("cliente").Where(x => t.Contains(x.idCuadre) && x.ordenFacturada == true).ToList();

                        ViewBag.tipopago = entity.ModoPago.ToList();
                        return PartialView("ListaReportesVentas", ReportesVentas_);
                    }
                    else
                    {
                        var ReportesVentas_ = entity.Venta.Include("cuadre").Include("factura").Include("usuario").Include("cliente").Where(x => t.Contains(x.idCuadre) && x.ordenFacturada == true && x.Factura.FirstOrDefault().numPago == rango.index).ToList();

                        ViewBag.tipopago = entity.ModoPago.ToList();
                        return PartialView("ListaReportesVentas", ReportesVentas_);
                    }






                }


            }
            catch (Exception ex)
            {

                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }



        #endregion

        #region NCF

        public ActionResult Ncf()
        {
            using (var Context = new barbdEntities())
            {
                ViewBag.cantidadExistente = Context.ComprobanteFiscal.Where(x => x.Utilizado == false).Count();

            }

            return PartialView();

        }

        [HttpPost]
        public ActionResult Ncf(FormCollection forms)
        {

            string NumeroDeSolicitudERROR = "";
            try
            {

                if (Request != null)
                {
                    HttpPostedFileBase file = Request.Files["file"];

                    if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                    {
                        string e = System.IO.Path.GetExtension(file.FileName);

                        if (e == ".csv")
                        {

                            List<ComprobanteFiscal> Listcomprobante = new List<ComprobanteFiscal>();
                            ComprobanteFiscal objComprobante;


                            var filePath = string.Empty;
                            string conString = string.Empty;
                            string path = Server.MapPath("~/Uploads/");
                            filePath = path + file.FileName;
                            file.SaveAs(filePath);
                            string csvData = System.IO.File.ReadAllText(filePath);

                            string[] org = csvData.Split('\n');

                            foreach (var item in org)
                            {
                                if (item.Count() > 5)
                                {
                                    objComprobante = new ComprobanteFiscal
                                    {
                                        NCF = item.Replace("\r", "").ToString().Trim(),
                                        Utilizado = false
                                    };

                                    Listcomprobante.Add(objComprobante);

                                }

                            }

                            if (Listcomprobante.Count > 1)
                            {



                                using (var Context = new barbdEntities())
                                {
                                    var l = Listcomprobante.ToList().Select(x => x.NCF);

                                    var validar = Context.ComprobanteFiscal.Where(x => l.Contains(x.NCF)).Count();

                                    if (validar == 0)
                                    {

                                        Context.ComprobanteFiscal.AddRange(Listcomprobante);
                                        Context.SaveChanges();


                                        var Info = new InfoMensaje
                                        {
                                            Tipo = "Success",
                                            Mensaje = Listcomprobante.Count + " NCF Registrados"
                                        };

                                        ViewBag.cantidadExistente = Context.ComprobanteFiscal.Where(x => x.Utilizado == false).Count();
                                        ViewData["Alert"] = Info;
                                        return PartialView();

                                    }
                                    else
                                    {
                                        var Info = new InfoMensaje
                                        {
                                            Tipo = "Warning",
                                            Mensaje = "Hay Ncf que ya existen en el sistema"
                                        };
                                        ViewData["Alert"] = Info;
                                        ViewBag.cantidadExistente = Context.ComprobanteFiscal.Where(x => x.Utilizado == false).Count();
                                        return PartialView();
                                    }


                                }

                            }
                            else
                            {
                                var Info = new InfoMensaje
                                {
                                    Tipo = "Warning",
                                    Mensaje = "No existen comprobante fiscal"
                                };
                                ViewData["Alert"] = Info;


                            }

                            return PartialView();

                        }
                        else
                        {
                            var Info = new InfoMensaje
                            {
                                Tipo = "Warning",
                                Mensaje = "Archivo no tiene formato .csv"
                            };
                            ViewData["Alert"] = Info;

                        }
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Adjunte el archivo"
                        };
                        ViewData["Alert"] = Info;

                    }


                }

                return PartialView();

            }
            catch (Exception ex)
            {


                var Info = new InfoMensaje
                {
                    Tipo = "Error",
                    Mensaje = $"{ex.Message} No. Orden:{NumeroDeSolicitudERROR}"
                };
                ViewData["Alert"] = Info;

                return PartialView();

            }


        }

        #endregion

        #region Reimprimir

        [HttpPost]
        public int Prefacturar(int id)
        {

            //using (barbdEntities context1 = new barbdEntities())
            //{

            //    int ValidarDetalles = context1.DetalleVenta.Where(x => x.idVenta == id).Count();

            //    if (ValidarDetalles == 0)
            //    {

            //        var V = context1.Venta.Find(id);

            //        V.ordenCerrada = true;
            //        V.ordenFacturada = true;

            //        context1.SaveChanges();

            //        Venta _venta = context1.Venta.Find(id);

            //        Factura factura = new Factura()
            //        {

            //            fecha = DateTime.Now,
            //            IVA = 18,
            //            idVenta = _venta.idVenta,
            //            // numFactura = venta.Factura[index].numFactura,
            //            numPago = 1,
            //            TieneCedito = false,
            //            total = 0
            //        };
            //        context1.Factura.Add(factura);
            //        context1.SaveChanges();

            //        return 0;

            //    }


            //}

            if (propina_.ToUpper() == "SI")
            {

                int result = 0;
                string empresa;
                string rnc;
                string telefono;
                string saludo;
                string cliente;
                string vendedor;
                decimal subtotal;
                decimal itbis;
                decimal propina;
                string[][] data;
                decimal Dollar;
                decimal Euro;

                using (barbdEntities context = new barbdEntities())
                {
                    empresa = context.Configuraciones.Find("Empresa").Value;
                    rnc = context.Configuraciones.Find("RNC").Value;
                    telefono = context.Configuraciones.Find("Telefono").Value;
                    saludo = context.Configuraciones.Find("Saludo").Value;
                    var d = context.Configuraciones.Find("Dollar").Value;
                    Dollar = Convert.ToDecimal(d);
                    var e = context.Configuraciones.Find("Euro").Value;
                    Euro = Convert.ToDecimal(e);

                    Venta venta = context.Venta.Find(id);
                    venta.ordenCerrada = true;

                    venta.Cliente.idMesa = null;

                    result = context.SaveChanges();
                    cliente = string.IsNullOrEmpty(venta.Cliente.nMesa) ? "0" : venta.Cliente.nMesa;// "xxx";//context.Cliente.Find(venta.idCliente).nombre;
                    vendedor = context.Usuario.Find(venta.idUsuario).nombre;
                    subtotal = (decimal)context.DetalleVenta.Where(vd => vd.idVenta == id).Sum(vd => vd.subTotal);
                    itbis = subtotal * 0.18m;
                    //propina = context.DetalleVenta.Where(vd => vd.idVenta == id).Sum(vd => vd.precioVenta).GetValueOrDefault(0) * 0.10m;
                    data =
                        context.DetalleVenta
                        .Where(vd => vd.idVenta == id)
                        .AsEnumerable()
                        .Select(vd => new string[5] {
                        "",
                        vd.Producto.nombre.ToUpper(),
                        Math.Round((decimal)vd.cantidad, 2).ToString("#,0"),
                        Math.Round((decimal)vd.precioVenta, 2).ToString("$#,0.00"),
                        Math.Round((decimal)vd.subTotal, 2).ToString("$#,0.00")
                        })
                        .ToArray();
                }

                Printer printer = new Printer();

                IDictionary<string, string> list1 = new Dictionary<string, string>();
                list1.Add("Cliente", cliente.ToUpper());
                list1.Add("Orden", id.ToString());
                list1.Add("Vendedor/a", vendedor.ToUpper());
                list1.Add("RNC", rnc);
                list1.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy"));
                list1.Add("Hora", DateTime.Now.ToString("hh:mm:ss tt"));

                Dictionary<string, string> tableDetails = new Dictionary<string, string>();
                Dictionary<string, string> tableTotal = new Dictionary<string, string>();
                if (itbis_.ToUpper() == "SI")
                {

                    tableDetails.Add("Subtotal", (subtotal - itbis).ToString("$#,0.00"));
                    tableDetails.Add("ITBIS %18", itbis.ToString("$#,0.00"));
                    //tableDetails.Add("Propina %10", propina.ToString("$#,0.00"));

                    tableTotal.Add("TOTAL", subtotal.ToString("$#,0.00"));

                }
                else
                {
                    tableTotal.Add("TOTAL", subtotal.ToString("$#,0.00"));
                }

                printer.AddTitle("Prefactura");
                printer.AddSpace(2);
                printer.AddString(empresa, true, System.Drawing.StringAlignment.Center);
                printer.AddString(telefono, alignment: System.Drawing.StringAlignment.Center);
                printer.AddSpace(2);
                printer.AddSubtitle("Información general");
                printer.AddSpace();
                printer.AddDescriptionList(list1, 2);
                printer.AddSpace(2);
                printer.AddSubtitle("Productos");
                printer.AddSpace();
                printer.AddTable(new string[4] { "Producto", "Cant.", "Precio", "Subtotal" }, data, true, map: new float[4] { 35f, 15f, 25f, 25f });
                printer.AddSpace();
                printer.AddTableDetails(tableDetails, 4);
                printer.AddSpace();
                printer.AddTableDetails(tableTotal, 4);
                printer.AddSpace(2);
                printer.AddBarCode(id.ToString());
                printer.AddString(saludo, alignment: System.Drawing.StringAlignment.Center);
                printer.AddSpace(2);
                decimal DollarInfo = subtotal / Dollar;
                printer.AddString("Dollar $ " + DollarInfo.ToString("$#,0.00"), alignment: System.Drawing.StringAlignment.Center);
                decimal EuroInfo = subtotal / Euro;
                printer.AddString("Euro $ " + EuroInfo.ToString("$#,0.00"), alignment: System.Drawing.StringAlignment.Center);

                printer.Print();

                return 1;

            }
            else
            {

                int result = 0;
                string empresa;
                string rnc;
                string telefono;
                string saludo;
                string cliente;
                string vendedor;
                decimal subtotal;
                decimal itbis;
                decimal propina;
                string[][] data;
                decimal Dollar;
                decimal Euro;

                using (barbdEntities context = new barbdEntities())
                {
                    empresa = context.Configuraciones.Find("Empresa").Value;
                    rnc = context.Configuraciones.Find("RNC").Value;
                    telefono = context.Configuraciones.Find("Telefono").Value;
                    saludo = context.Configuraciones.Find("Saludo").Value;
                    var d = context.Configuraciones.Find("Dollar").Value;
                    Dollar = Convert.ToDecimal(d);
                    var e = context.Configuraciones.Find("Euro").Value;
                    Euro = Convert.ToDecimal(e);

                    Venta venta = context.Venta.Find(id);
                    venta.ordenCerrada = true;

                    venta.Cliente.idMesa = null;

                    result = context.SaveChanges();
                    cliente = string.IsNullOrEmpty(venta.Cliente.nMesa) ? "0" : venta.Cliente.nMesa;// "xxx";//context.Cliente.Find(venta.idCliente).nombre;
                    vendedor = context.Usuario.Find(venta.idUsuario).nombre;
                    subtotal = (decimal)context.DetalleVenta.Where(vd => vd.idVenta == id).Sum(vd => vd.subTotal);
                    itbis = subtotal * 0.18m;
                    propina = subtotal * 0.10m;
                    data =
                        context.DetalleVenta
                        .Where(vd => vd.idVenta == id)
                        .AsEnumerable()
                        .Select(vd => new string[5] {
                        "",
                        vd.Producto.nombre.ToUpper(),
                        Math.Round((decimal)vd.cantidad, 2).ToString("#,0"),
                        Math.Round((decimal)vd.precioVenta, 2).ToString("$#,0.00"),
                        Math.Round((decimal)vd.subTotal, 2).ToString("$#,0.00")
                        })
                        .ToArray();
                }

                Printer printer = new Printer();

                IDictionary<string, string> list1 = new Dictionary<string, string>();
                list1.Add("Cliente", cliente.ToUpper());
                list1.Add("Orden", id.ToString());
                list1.Add("Vendedor/a", vendedor.ToUpper());
                list1.Add("RNC", rnc);
                list1.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy"));
                list1.Add("Hora", DateTime.Now.ToString("hh:mm:ss tt"));

                Dictionary<string, string> tableDetails = new Dictionary<string, string>();
                tableDetails.Add("Subtotal", (subtotal - itbis - propina).ToString("$#,0.00"));
                tableDetails.Add("ITBIS %18", itbis.ToString("$#,0.00"));
                tableDetails.Add("Propina %10", propina.ToString("$#,0.00"));
                Dictionary<string, string> tableTotal = new Dictionary<string, string>();
                tableTotal.Add("TOTAL", subtotal.ToString("$#,0.00"));

                printer.AddTitle("Prefactura");
                printer.AddSpace(2);
                printer.AddString(empresa, true, System.Drawing.StringAlignment.Center);
                printer.AddString(telefono, alignment: System.Drawing.StringAlignment.Center);
                printer.AddSpace(2);
                printer.AddSubtitle("Información general");
                printer.AddSpace();
                printer.AddDescriptionList(list1, 2);
                printer.AddSpace(2);
                printer.AddSubtitle("Productos");
                printer.AddSpace();
                printer.AddTable(new string[4] { "Producto", "Cant.", "Precio", "Subtotal" }, data, true, map: new float[4] { 35f, 15f, 25f, 25f });
                printer.AddSpace();
                printer.AddTableDetails(tableDetails, 4);
                printer.AddSpace();
                printer.AddTableDetails(tableTotal, 4);
                printer.AddSpace(2);
                printer.AddBarCode(id.ToString());
                printer.AddString(saludo, alignment: System.Drawing.StringAlignment.Center);
                printer.AddSpace(2);
                decimal DollarInfo = subtotal / Dollar;
                printer.AddString("Dollar $ " + DollarInfo.ToString("$#,0.00"), alignment: System.Drawing.StringAlignment.Center);
                decimal EuroInfo = subtotal / Euro;
                printer.AddString("Euro $ " + EuroInfo.ToString("$#,0.00"), alignment: System.Drawing.StringAlignment.Center);

                printer.Print();

                return 1;
            }
        }

        public int PrefacturarCredito(int id)
        {

            //using (barbdEntities context1 = new barbdEntities())
            //{

            //    int ValidarDetalles = context1.DetalleVenta.Where(x => x.idVenta == id).Count();

            //    if (ValidarDetalles == 0)
            //    {

            //        var V = context1.Venta.Find(id);

            //        V.ordenCerrada = true;
            //        V.ordenFacturada = true;

            //        context1.SaveChanges();

            //        Venta _venta = context1.Venta.Find(id);

            //        Factura factura = new Factura()
            //        {

            //            fecha = DateTime.Now,
            //            IVA = 18,
            //            idVenta = _venta.idVenta,
            //            // numFactura = venta.Factura[index].numFactura,
            //            numPago = 1,
            //            TieneCedito = false,
            //            total = 0
            //        };
            //        context1.Factura.Add(factura);
            //        context1.SaveChanges();

            //        return 0;

            //    }


            //}


            if (propina_.ToUpper() == "SI")
            {


                int result = 0;
                string empresa;
                string rnc;
                string telefono;
                string saludo;
                string cliente;
                string vendedor;
                decimal subtotal;
                decimal itbis;
                string[][] data;
                decimal descuento;
                string des;


                using (barbdEntities context = new barbdEntities())
                {
                    var f = context.Factura.Find(id);

                    id = Convert.ToInt32(f.idVenta);


                    empresa = context.Configuraciones.Find("Empresa").Value;

                    telefono = context.Configuraciones.Find("Telefono").Value;
                    saludo = context.Configuraciones.Find("Saludo").Value;

                    Venta venta = context.Venta.Find(id);

                    venta.ordenCerrada = true;


                    rnc = String.IsNullOrEmpty(venta.Cliente.rnc) ? "" : venta.Cliente.rnc.ToString();

                    venta.Cliente.idMesa = null;

                    result = context.SaveChanges();
                    cliente = context.Cliente.Find(venta.idCliente).nMesa;
                    vendedor = context.Usuario.Find(venta.idUsuario).nombre;
                    subtotal = (decimal)context.DetalleVenta.Where(vd => vd.idVenta == id).Sum(vd => vd.subTotal);
                    descuento = subtotal - (decimal)venta.Factura.Sum(ff => ff.total);// (decimal)factura_.descuento;
                    des = context.Factura.Where(x => x.idVenta == id).First().descuento.ToString();
                    itbis = subtotal * 0.18m;
                    data =
                        context.DetalleVenta
                        .Where(vd => vd.idVenta == id)
                        .AsEnumerable()
                        .Select(vd => new string[5] {
                        "",
                        vd.Producto.nombre.ToUpper(),
                        Math.Round((decimal)vd.cantidad, 2).ToString("#,0"),
                        Math.Round((decimal)vd.precioVenta, 2).ToString("$#,0.00"),
                        Math.Round((decimal)vd.subTotal, 2).ToString("$#,0.00")
                        })
                        .ToArray();


                    Printer printer = new Printer();

                    IDictionary<string, string> list1 = new Dictionary<string, string>();
                    list1.Add("Cliente", cliente.ToUpper());
                    list1.Add("Orden", id.ToString());
                    list1.Add("Vendedor/a", vendedor.ToUpper());
                    list1.Add("RNC", rnc);
                    if (string.IsNullOrEmpty(rnc))
                    {
                        list1.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy"));
                        list1.Add("Hora", DateTime.Now.ToString("hh:mm:ss tt"));
                    }
                    else
                    {
                        list1.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy hh:mm:ss tt"));
                        var NCF_ = context.ComprobanteFiscal.Where(x => x.Utilizado == false).Count() > 0 ? context.ComprobanteFiscal.Where(x => x.Utilizado == false).First().NCF : "";
                        list1.Add("NCF", NCF_);

                        if (!String.IsNullOrEmpty(NCF_))
                        {
                            var ACTRNC = context.ComprobanteFiscal.Where(X => X.NCF == NCF_).First();
                            ACTRNC.Utilizado = true;
                            context.SaveChanges();
                        }


                    }

                    Dictionary<string, string> tableDetails = new Dictionary<string, string>();
                    Dictionary<string, string> tableTotal = new Dictionary<string, string>();

                    if (itbis_.ToUpper() == "SI")
                    {
                       
                        tableDetails.Add("Subtotal", (subtotal - itbis).ToString("$#,0.00"));
                        tableDetails.Add("ITBIS", itbis.ToString("$#,0.00"));

                        tableDetails.Add("Descuento", descuento.ToString("$#,0.00") + " (" + des.ToString() + "%)");

                        tableTotal.Add("TOTAL", (subtotal - descuento).ToString("$#,0.00"));

                    }
                    else
                    {
                        tableDetails.Add("Descuento", descuento.ToString("$#,0.00") + " (" + des.ToString() + "%)");

                        tableTotal.Add("TOTAL", (subtotal - descuento).ToString("$#,0.00"));
                    }


                    printer.AddTitle("Factura Credito");
                    printer.AddSpace(2);
                    printer.AddString(empresa, true, System.Drawing.StringAlignment.Center);
                    printer.AddString(telefono, alignment: System.Drawing.StringAlignment.Center);
                    printer.AddSpace(2);
                    printer.AddSubtitle("Información general");
                    printer.AddSpace();
                    printer.AddDescriptionList(list1, 2);
                    printer.AddSpace(2);
                    printer.AddSubtitle("Productos");
                    printer.AddSpace();
                    printer.AddTable(new string[4] { "Producto", "Cant.", "Precio", "Subtotal" }, data, true, map: new float[4] { 35f, 15f, 25f, 25f });
                    printer.AddSpace();
                    printer.AddTableDetails(tableDetails, 4);
                    printer.AddSpace();
                    printer.AddTableDetails(tableTotal, 4);
                    printer.AddSpace(2);
                    printer.AddBarCode(id.ToString());
                    printer.AddString(saludo, alignment: System.Drawing.StringAlignment.Center);
                    printer.AddSpace(2);

                    printer.Print();

                    return 1;
                }
            }
            else
            {
                int result = 0;
                string empresa;
                string rnc;
                string telefono;
                string saludo;
                string cliente;
                string vendedor;
                decimal subtotal;
                decimal itbis;
                string[][] data;
                decimal descuento;
                string des;
                decimal propina;


                using (barbdEntities context = new barbdEntities())
                {
                    var f = context.Factura.Find(id);

                    id = Convert.ToInt32(f.idVenta);


                    empresa = context.Configuraciones.Find("Empresa").Value;

                    telefono = context.Configuraciones.Find("Telefono").Value;
                    saludo = context.Configuraciones.Find("Saludo").Value;

                    Venta venta = context.Venta.Find(id);

                    venta.ordenCerrada = true;


                    rnc = String.IsNullOrEmpty(venta.Cliente.rnc) ? "" : venta.Cliente.rnc.ToString();

                    venta.Cliente.idMesa = null;

                    result = context.SaveChanges();
                    cliente = context.Cliente.Find(venta.idCliente).nMesa;
                    vendedor = context.Usuario.Find(venta.idUsuario).nombre;
                    subtotal = (decimal)context.DetalleVenta.Where(vd => vd.idVenta == id).Sum(vd => vd.subTotal);
                    descuento = subtotal - (decimal)venta.Factura.Sum(ff => ff.total);// (decimal)factura_.descuento;
                    des = context.Factura.Where(x => x.idVenta == id).First().descuento.ToString();
                    itbis = subtotal * 0.18m;
                    propina = subtotal * 0.10m;

                    data =
                        context.DetalleVenta
                        .Where(vd => vd.idVenta == id)
                        .AsEnumerable()
                        .Select(vd => new string[5] {
                        "",
                        vd.Producto.nombre.ToUpper(),
                        Math.Round((decimal)vd.cantidad, 2).ToString("#,0"),
                        Math.Round((decimal)vd.precioVenta, 2).ToString("$#,0.00"),
                        Math.Round((decimal)vd.subTotal, 2).ToString("$#,0.00")
                        })
                        .ToArray();


                    Printer printer = new Printer();

                    IDictionary<string, string> list1 = new Dictionary<string, string>();
                    list1.Add("Cliente", cliente.ToUpper());
                    list1.Add("Orden", id.ToString());
                    list1.Add("Vendedor/a", vendedor.ToUpper());
                    list1.Add("RNC", rnc);
                    if (string.IsNullOrEmpty(rnc))
                    {
                        list1.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy"));
                        list1.Add("Hora", DateTime.Now.ToString("hh:mm:ss tt"));
                    }
                    else
                    {
                        list1.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy hh:mm:ss tt"));
                        var NCF_ = context.ComprobanteFiscal.Where(x => x.Utilizado == false).Count() > 0 ? context.ComprobanteFiscal.Where(x => x.Utilizado == false).First().NCF : "";
                        list1.Add("NCF", NCF_);

                        if (!String.IsNullOrEmpty(NCF_))
                        {
                            var ACTRNC = context.ComprobanteFiscal.Where(X => X.NCF == NCF_).First();
                            ACTRNC.Utilizado = true;
                            context.SaveChanges();
                        }


                    }

                    Dictionary<string, string> tableDetails = new Dictionary<string, string>();
                    tableDetails.Add("Subtotal", (subtotal - itbis- propina).ToString("$#,0.00"));
                    tableDetails.Add("ITBIS %18", itbis.ToString("$#,0.00"));
                    tableDetails.Add("Propina %10", itbis.ToString("$#,0.00"));
                    tableDetails.Add("Descuento", descuento.ToString("$#,0.00") + " (" + des.ToString() + "%)");
                    Dictionary<string, string> tableTotal = new Dictionary<string, string>();
                    tableTotal.Add("TOTAL", (subtotal - descuento).ToString("$#,0.00"));


                    printer.AddTitle("Factura Credito");
                    printer.AddSpace(2);
                    printer.AddString(empresa, true, System.Drawing.StringAlignment.Center);
                    printer.AddString(telefono, alignment: System.Drawing.StringAlignment.Center);
                    printer.AddSpace(2);
                    printer.AddSubtitle("Información general");
                    printer.AddSpace();
                    printer.AddDescriptionList(list1, 2);
                    printer.AddSpace(2);
                    printer.AddSubtitle("Productos");
                    printer.AddSpace();
                    printer.AddTable(new string[4] { "Producto", "Cant.", "Precio", "Subtotal" }, data, true, map: new float[4] { 35f, 15f, 25f, 25f });
                    printer.AddSpace();
                    printer.AddTableDetails(tableDetails, 4);
                    printer.AddSpace();
                    printer.AddTableDetails(tableTotal, 4);
                    printer.AddSpace(2);
                    printer.AddBarCode(id.ToString());
                    printer.AddString(saludo, alignment: System.Drawing.StringAlignment.Center);
                    printer.AddSpace(2);

                    printer.Print();

                    return 1;
                }
            }
        }


        public ActionResult Reimprimir()
        {

            using (var entity = new barbdEntities())
            {


                List<Venta> v = new List<Venta>();

                var idcuadre = entity.Cuadre.SingleOrDefault(x => x.cerrado == false);


                var ReportesVentas_ = entity.Venta.Include("usuario").Include("cliente").Include("detalleventa").Where(x => x.idCuadre == idcuadre.idCuadre && x.ordenCerrada == true && x.ordenFacturada != true).ToList();

                ViewData["Producto"] = entity.Producto.ToList();

                ViewBag.tipopago = entity.ModoPago.ToList();
                return PartialView(ReportesVentas_);

            }
        }
        [HttpPost]
        public ActionResult ReimprimirSearch(Venta venta)
        {

            using (var entity = new barbdEntities())
            {


                List<Venta> v = new List<Venta>();

                var idcuadre = entity.Cuadre.SingleOrDefault(x => x.cerrado == false);


                var ReportesVentas_ = entity.Venta.Include("usuario").Include("cliente").Include("detalleventa").Where(x => x.idCuadre == idcuadre.idCuadre && x.ordenCerrada == true && x.idVenta == venta.idVenta && x.ordenFacturada != true).ToList();

                ViewData["Producto"] = entity.Producto.ToList();

                ViewBag.tipopago = entity.ModoPago.ToList();
                return PartialView("Reimprimir", ReportesVentas_);

            }
        }


        [HttpPost]
        public ActionResult ModificarProductoCarrito(int idDetalle, decimal idventa, int cantidad_)
        {

            using (barbdEntities context = new barbdEntities())
            {

                var validarCantidad = context.DetalleVenta.Find(idDetalle);

                if (cantidad_ == 0 || cantidad_ >= validarCantidad.cantidad)
                {
                    string error = "Error";
                    return Json(new { error });

                }

                var validarEspecial = context.DetallesVentaEspecial.Where(x => x.idDetalle == idDetalle).Count();

                if (validarEspecial > 0)
                {
                    var detalleVenta = context.DetalleVenta.Find(idDetalle);
                    Producto producto = context.Producto.Find(detalleVenta.idProducto);

                    var detalleE = context.DetallesVentaEspecial.Where(x => x.idDetalle == idDetalle).ToList();
                    context.DetallesVentaEspecial.RemoveRange(detalleE);

                    //DetalleVenta detalle = context.DetalleVenta.Find(idDetalle);
                    //var pp = context.Producto.Find(detalle.idProducto);
                    //context.Entry(detalle).State = System.Data.Entity.EntityState.Deleted;

                    //Venta venta = context.Venta.Find(detalle.idVenta);
                    //venta.total -= detalle.subTotal;

                    context.SaveChanges();

                    if (producto.especial == true)
                    {
                        int contador = 0;

                        if (!string.IsNullOrEmpty(producto.idProductoEspecial))
                        {
                            string[] ipe = producto.idProductoEspecial.Split(',');
                            string[] ipec = producto.idProductoEspecialCantidad.Split(',');



                            foreach (var produc in ipe)
                            {
                                var Oproducto = new Producto();

                                var p = context.Producto.Find(ipe[contador]);

                                Oproducto.idProducto = p.idProducto;
                                Oproducto.nombre = p.nombre;

                                foreach (var cantidad in ipec)
                                {

                                    Oproducto.idProductoEspecialCantidad = ipec[contador];

                                }


                                List<Models.Inventario> queryInventario = context.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleBar").ToList();
                                //bool productoExiste = queryInventario.Any(i => i.IdProducto == Oproducto.idProducto);
                                int cantidadInventario = queryInventario.SingleOrDefault(i => i.IdProducto == Oproducto.idProducto) != null ? queryInventario.SingleOrDefault(i => i.IdProducto == Oproducto.idProducto).Cantidad : 0;
                                if (cantidadInventario < int.Parse(Oproducto.idProductoEspecialCantidad) * detalleVenta.cantidad)
                                {
                                    string error =
                                        cantidadInventario < 1
                                        ? "No se puede agregar el producto, no quedan en el local " + Oproducto.nombre + ""
                                        : "No se puede agregar el producto, solo quedan " + cantidadInventario + " " + Oproducto.nombre + " en el local";

                                    return Json(new { error });



                                }

                                contador += 1;

                            }

                            detalleVenta.subTotal = (float)(cantidad_ * producto.precioVenta);
                            detalleVenta.despachada = false; //Context.DetalleVenta.Find(detalleVenta.idDetalle).despachada == true ? "Si" : "No";
                            detalleVenta.precioVenta = producto.precioVenta;
                            detalleVenta.precioEntrada = producto.precioAlmacen;
                            detalleVenta.cantidad = cantidad_;
                            detalleVenta.despachada = true;
                            //detalleVenta.idEspecial = objDetallesEspecial.idEspecial;

                            context.DetalleVenta.Add(detalleVenta);


                            Venta venta = context.Venta.Find(detalleVenta.idVenta);
                            venta.total += detalleVenta.subTotal;
                            context.Entry(venta).State = System.Data.Entity.EntityState.Modified;

                            context.SaveChanges();


                            //agregar producto

                            contador = 0;


                            foreach (var produc in ipe)
                            {
                                var Oproducto = new Producto();

                                var p = context.Producto.Find(ipe[contador]);

                                Oproducto.idProducto = p.idProducto;
                                Oproducto.precioAlmacen = p.precioAlmacen;

                                foreach (var cantidad in ipec)
                                {

                                    Oproducto.idProductoEspecialCantidad = ipec[contador];

                                }
                                var objDetallesEspecial = new DetallesVentaEspecial
                                {
                                    idVenta = detalleVenta.idVenta,
                                    idProducto = Oproducto.idProducto,
                                    cantidad = int.Parse(Oproducto.idProductoEspecialCantidad) * cantidad_,
                                    idCuadre = context.Cuadre.SingleOrDefault(x => x.cerrado == false).idCuadre,
                                    idDetalle = detalleVenta.idDetalle,
                                    precioEntrada = Oproducto.precioAlmacen
                                };

                                context.DetallesVentaEspecial.Add(objDetallesEspecial);
                                context.SaveChanges();

                                contador += 1;
                            };

                            DetalleVenta detalle = context.DetalleVenta.Find(idDetalle);
                            context.Entry(detalle).State = System.Data.Entity.EntityState.Deleted;

                            Venta venta_ = context.Venta.Find(detalle.idVenta);
                            venta_.total -= detalle.subTotal;

                            context.SaveChanges();

                            //return Json(new
                            //{
                            //    producto = new Producto()
                            //    {
                            //        idProducto = producto.idProducto,
                            //        nombre = producto.nombre,
                            //        precioVenta = producto.precioVenta,


                            //    },
                            //    idDetalle = detalleVenta.idDetalle
                            //}, JsonRequestBehavior.AllowGet);



                        }
                        else
                        {
                            string error = "Especial no tiene combo agregado";
                            return Json(new { error });
                        }


                    }



                }
                else
                {

                    var detalleVenta = context.DetalleVenta.Find(idDetalle);

                    Producto producto = context.Producto.Find(detalleVenta.idProducto);

                    float subt = detalleVenta.subTotal;

                    detalleVenta.subTotal = (float)(cantidad_ * producto.precioVenta);
                    detalleVenta.despachada = true; //Context.DetalleVenta.Find(detalleVenta.idDetalle).despachada == true ? "Si" : "No";
                    detalleVenta.precioVenta = producto.precioVenta;
                    detalleVenta.precioEntrada = producto.precioAlmacen;
                    detalleVenta.cantidad = cantidad_;

                    context.Entry(detalleVenta).State = System.Data.Entity.EntityState.Modified;

                    context.SaveChanges();

                    Venta venta = context.Venta.Find(detalleVenta.idVenta);
                    venta.total -= subt;
                    venta.total += detalleVenta.subTotal;
                    context.Entry(venta).State = System.Data.Entity.EntityState.Modified;

                    context.SaveChanges();

                    //return Json(new
                    //{
                    //    producto = new Producto()
                    //    {
                    //        idProducto = producto.idProducto,
                    //        nombre = producto.nombre,
                    //        precioVenta = producto.precioVenta,

                    //    },
                    //    idDetalle = detalleVenta.idDetalle
                    //}, JsonRequestBehavior.AllowGet);


                }



                List<Venta> v = new List<Venta>();

                var idcuadre = context.Cuadre.SingleOrDefault(x => x.cerrado == false);


                var ReportesVentas_ = context.Venta.Include("usuario").Include("cliente").Include("detalleventa").Where(x => x.idCuadre == idcuadre.idCuadre && x.ordenCerrada == true && x.idVenta == idventa && x.ordenFacturada != true).ToList();

                ViewData["Producto"] = context.Producto.ToList();

                ViewBag.tipopago = context.ModoPago.ToList();
                return PartialView("Reimprimir", ReportesVentas_);


            }




        }


        [HttpPost]
        public ActionResult EliminarProductoCarrito(int idDetalle, decimal idventa)
        {


            using (barbdEntities context = new barbdEntities())
            {
                var Confi = context.Configuraciones.Where(x => x.Key == "Empresa").SingleOrDefault();
                var validarEspecial = context.DetallesVentaEspecial.Where(x => x.idDetalle == idDetalle).Count();


                if (validarEspecial > 0)
                {
                    var detalleE = context.DetallesVentaEspecial.Where(x => x.idDetalle == idDetalle).ToList();
                    context.DetallesVentaEspecial.RemoveRange(detalleE);


                    DetalleVenta detalle = context.DetalleVenta.Find(idDetalle);
                    var p = context.Producto.Find(detalle.idProducto);
                    context.Entry(detalle).State = System.Data.Entity.EntityState.Deleted;


                    Venta venta = context.Venta.Find(detalle.idVenta);
                    venta.total -= detalle.subTotal;

                    ProductoEliminado ObjProductoEliminado = new ProductoEliminado
                    {
                        NoOrden = detalle.idVenta.ToString(),
                        IdProducto = p.idProducto,
                        Cantidad = detalle.cantidad,
                        Precio = detalle.precioVenta,
                        IdCuadre = venta.idCuadre

                    };

                    context.ProductoEliminado.Add(ObjProductoEliminado);

                    context.SaveChanges();

                    Printer printerr = new Printer();
                    decimal total = Convert.ToDecimal(detalle.cantidad) * Convert.ToDecimal(detalle.precioVenta);
                    printerr.CorreoEliminado("<html><head><style>table {  font-family: arial, sans-serif;  border-collapse: collapse;  width: 100%;}td, th {  border: 1px solid #dddddd;  text-align: left;  padding: 8px;}tr:nth-child(even) { background-color: #dddddd;}</style></head><body><h2>Producto Eliminado (" + Confi.Value.ToString() + ")</h2><table>  <tr>    <th>No.Orden</th>    <th>Producto</th>    <th>Cant.</th>    <th>Precio</th>    <th>Sub-Total</th>  </tr>  <tr>    <td>" + detalle.idVenta.ToString() + "</td>    <td>" + p.nombre.ToString() + "</td>    <td>" + detalle.cantidad.ToString() + "</td>    <td>" + detalle.precioVenta.ToString() + "</td>    <td>" + total.ToString() + "</td>  </tr></table>  <p><b>Multicore</b>.Reportes</p></body></html>");




                }
                else
                {

                    DetalleVenta detalle = context.DetalleVenta.Find(idDetalle);
                    var p = context.Producto.Find(detalle.idProducto);
                    context.Entry(detalle).State = System.Data.Entity.EntityState.Deleted;


                    Venta venta = context.Venta.Find(detalle.idVenta);
                    venta.total -= detalle.subTotal;
                    context.SaveChanges();

                    ProductoEliminado ObjProductoEliminado = new ProductoEliminado
                    {
                        NoOrden = detalle.idVenta.ToString(),
                        IdProducto = p.idProducto,
                        Cantidad = detalle.cantidad,
                        Precio = detalle.precioVenta,
                        IdCuadre = venta.idCuadre

                    };

                    context.ProductoEliminado.Add(ObjProductoEliminado);

                    Printer printerr = new Printer();
                    decimal total = Convert.ToDecimal(detalle.cantidad) * Convert.ToDecimal(detalle.precioVenta);
                    printerr.CorreoEliminado("<html><head><style>table {  font-family: arial, sans-serif;  border-collapse: collapse;  width: 100%;}td, th {  border: 1px solid #dddddd;  text-align: left;  padding: 8px;}tr:nth-child(even) { background-color: #dddddd;}</style></head><body><h2>Producto Eliminado (" + Confi.Value.ToString() + ")</h2><table>  <tr>    <th>No.Orden</th>    <th>Producto</th>    <th>Cant.</th>    <th>Precio</th>    <th>Sub-Total</th>  </tr>  <tr>    <td>" + detalle.idVenta.ToString() + "</td>    <td>" + p.nombre.ToString() + "</td>    <td>" + detalle.cantidad.ToString() + "</td>    <td>" + detalle.precioVenta.ToString() + "</td>    <td>" + total.ToString() + "</td>  </tr></table>  <p><b>Multicore</b>.Reportes</p></body></html>");




                }

                int ValidarDetalles = context.DetalleVenta.Where(x => x.idVenta == idventa).Count();

                if (ValidarDetalles == 0)
                {

                    var V = context.Venta.Find(idventa);

                    V.ordenCerrada = true;
                    V.ordenFacturada = true;
                    V.Cliente.idMesa = null;

                    context.SaveChanges();

                    Venta _venta = context.Venta.Find(idventa);

                    Factura factura = new Factura()
                    {

                        fecha = DateTime.Now,
                        IVA = 18,
                        idVenta = _venta.idVenta,
                        // numFactura = venta.Factura[index].numFactura,
                        numPago = 1,
                        TieneCedito = false,
                        total = 0
                    };
                    context.Factura.Add(factura);
                    context.SaveChanges();
                    // printer.CorreoEliminado("<html><head><style>table {  font-family: arial, sans-serif;  border-collapse: collapse;  width: 100%;}td, th {  border: 1px solid #dddddd;  text-align: left;  padding: 8px;}tr:nth-child(even) { background-color: #dddddd;}</style></head><body><h2>Producto Eliminado (Elite)</h2><table>  <tr>    <th>No.Orden</th>    <th>Producto</th>    <th>Cant.</th>    <th>Precio</th>    <th>Sub-Total</th>  </tr>  <tr>    <td>" + ValidarDetalles.idVenta.ToString() + "</td>    <td>" + detalle.Producto.nombre.ToString() + "</td>    <td>" + detalle.cantidad.ToString() + "</td>    <td>" + detalle.precioVenta.ToString() + "</td>    <td>" + detalle.idVenta * detalle.precioVenta + "</td>  </tr></table>  <p><b>Multicore</b>.Reportes</p></body></html>");
                    // return 0;

                }


                List<Venta> v = new List<Venta>();

                var idcuadre = context.Cuadre.SingleOrDefault(x => x.cerrado == false);


                var ReportesVentas_ = context.Venta.Include("usuario").Include("cliente").Include("detalleventa").Where(x => x.idCuadre == idcuadre.idCuadre && x.ordenCerrada == true && x.idVenta == idventa && x.ordenFacturada != true).ToList();

                ViewData["Producto"] = context.Producto.ToList();

                ViewBag.tipopago = context.ModoPago.ToList();
                return PartialView("Reimprimir", ReportesVentas_);



            }
        }
        [HttpPost]
        public ActionResult Reimprimir(int Id)
        {

            return View();

        }

        #endregion

    }


    #region OtrasClass

    public class arqueo
    {
        public string Descripcion { get; set; }
        public decimal Valor { get; set; }

        public int index { get; set; }

    }

    public class RangoFecha
    {
        public DateTime fechaInicio { get; set; }
        public DateTime fechaFinal { get; set; }

        public int index { get; set; }

    }

    public class VentasTotales
    {
        public float TotalVenta { get; set; }
        public decimal TotalGanancias { get; set; }

    }

    public class Localidad

    {
        public int Id { get; set; }
        public string Nombre { get; set; }
    }


    #endregion

}