using barApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace barApp.Controllers
{
    public class OrdenController : Controller
    {
       
        // GET: Orden
        public ActionResult Index()
        {

            if (Session["Usuario"] == null)
            {

                return RedirectToAction("Index", "Login");

            }

            using (var entity = new barbdEntities())
            {
                Session["idVenta"] = null;
                int idrol = Convert.ToInt32(Session["IdUsuario"]);
                ViewBag.ListadoClienteOrdenar = entity.Venta.Include("Cliente").Where(x => x.idUsuario == idrol && x.ordenCerrada == null).ToList();
                ViewData["Ordenar"] = null;
                ViewBag.Categoria = entity.Categoria.ToList();
                ViewBag.Producto = entity.Producto.Where(x => x.activo == true).ToList();
                ViewData["Mesas"] = entity.Mesa.ToList();
            }

            return View();
        }

        [HttpPost]
        public ActionResult IndexDetalles(string Id)
        {
            using (var entity = new barbdEntities())
            {
                int idVenta = Convert.ToInt32(Id);
                var ListaDetalleVenta = entity.DetalleVenta.Include("Venta").Include("Producto").Where(x => x.idVenta == idVenta).ToList();

                return PartialView("ListadoDeOrdenes", ListaDetalleVenta);
            }
        }

        public ActionResult Total(int Id)
        {
            using (var entity = new barbdEntities())
            {

                var ObjTotal = entity.Venta.Find(Id);

                return Json(ObjTotal.total, JsonRequestBehavior.AllowGet);

            }
        }

        [HttpPost]
        public ActionResult AgregarProductoCarrito(DetalleVenta detalleVenta)
        {
            using (var Context = new barbdEntities())
            {

   
                Producto producto = Context.Producto.Find(detalleVenta.idProducto);

                        
                //////////////Especiales

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

                            var p = Context.Producto.Find(ipe[contador]);

                            Oproducto.idProducto = p.idProducto;
                            Oproducto.nombre = p.nombre;

                            foreach (var cantidad in ipec)
                            {

                                Oproducto.idProductoEspecialCantidad = ipec[contador];

                            }


                            List<Models.Inventario> queryInventario = Context.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleBar").ToList();
                            //bool productoExiste = queryInventario.Any(i => i.IdProducto == Oproducto.idProducto);
                            int cantidadInventario = queryInventario.SingleOrDefault(i => i.IdProducto == Oproducto.idProducto) != null ? queryInventario.SingleOrDefault(i => i.IdProducto == Oproducto.idProducto).Cantidad : 0;
                            if (cantidadInventario < int.Parse(Oproducto.idProductoEspecialCantidad)*detalleVenta.cantidad)
                            {
                                string error =
                                    cantidadInventario < 1
                                    ? "No se puede agregar el producto, no quedan en el local "+Oproducto.nombre+""
                                    : "No se puede agregar el producto, solo quedan " + cantidadInventario+" "+ Oproducto.nombre+ " en el local";

                                return Json(new { error });



                            }

                            contador += 1;

                        }

                        detalleVenta.subTotal = (float)(detalleVenta.cantidad * producto.precioVenta);
                        detalleVenta.despachada = false; //Context.DetalleVenta.Find(detalleVenta.idDetalle).despachada == true ? "Si" : "No";
                        detalleVenta.precioVenta = producto.precioVenta;
                        detalleVenta.precioEntrada = producto.precioAlmacen;
                        //detalleVenta.idEspecial = objDetallesEspecial.idEspecial;

                        Context.DetalleVenta.Add(detalleVenta);

                        Venta venta = Context.Venta.Find(detalleVenta.idVenta);
                        venta.total += detalleVenta.subTotal;
                        Context.Entry(venta).State = System.Data.Entity.EntityState.Modified;

                        Context.SaveChanges();


                        //agregar producto

                        contador = 0;
              

                        foreach (var produc in ipe)
                        {
                            var Oproducto = new Producto();

                            var p = Context.Producto.Find(ipe[contador]);

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
                                cantidad = int.Parse(Oproducto.idProductoEspecialCantidad)* detalleVenta.cantidad,
                                idCuadre = Context.Cuadre.SingleOrDefault(x => x.cerrado == false).idCuadre,
                                idDetalle = detalleVenta.idDetalle,
                                precioEntrada = Oproducto.precioAlmacen
                            };

                            Context.DetallesVentaEspecial.Add(objDetallesEspecial);
                            Context.SaveChanges();

                            contador += 1;
                        };




                        return Json(new
                        {
                            producto = new Producto()
                            {
                                idProducto = producto.idProducto,
                                nombre = producto.nombre,
                                precioVenta = producto.precioVenta,

                            },
                            idDetalle = detalleVenta.idDetalle
                        }, JsonRequestBehavior.AllowGet);



                    }
                    else
                    {
                        string error = "Especial no tiene combo agregado";
                        return Json(new { error });
                    }

                  
                }          
                  

                
                else
                {




                    //////////////

                    List<Models.Inventario> queryInventario = Context.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleBar").ToList();
                    bool productoExiste = queryInventario.Any(i => i.IdProducto == producto.idProducto);
                    int cantidadInventario = queryInventario.SingleOrDefault(i => i.IdProducto == producto.idProducto) != null ? queryInventario.SingleOrDefault(i => i.IdProducto == producto.idProducto).Cantidad : 0;
                    if (!productoExiste || cantidadInventario < detalleVenta.cantidad)
                    {
                        string error =
                            cantidadInventario < 1
                            ? "No se puede agregar el producto, no quedan en el local"
                            : "No se puede agregar el producto, solo quedan " + cantidadInventario + " en el local";

                        return Json(new { error });
                    }

                    detalleVenta.subTotal = (float)(detalleVenta.cantidad * producto.precioVenta);
                    detalleVenta.despachada = false; //Context.DetalleVenta.Find(detalleVenta.idDetalle).despachada == true ? "Si" : "No";
                    detalleVenta.precioVenta = producto.precioVenta;
                    detalleVenta.precioEntrada = producto.precioAlmacen;

                    Context.DetalleVenta.Add(detalleVenta);

                    Venta venta = Context.Venta.Find(detalleVenta.idVenta);
                    venta.total += detalleVenta.subTotal;
                    Context.Entry(venta).State = System.Data.Entity.EntityState.Modified;

                    Context.SaveChanges();

                    return Json(new
                    {
                        producto = new Producto()
                        {
                            idProducto = producto.idProducto,
                            nombre = producto.nombre,
                            precioVenta = producto.precioVenta,

                        },
                        idDetalle = detalleVenta.idDetalle
                    }, JsonRequestBehavior.AllowGet);

                }
            }
        }

        [HttpPost]
        public int EliminarProductoCarrito(int idDetalle)
        {
            
            Printer printer = new Printer();
            string[][] data;

            using (barbdEntities context = new barbdEntities())
            {
                var validarEspecial = context.DetallesVentaEspecial.Where(x => x.idDetalle == idDetalle).Count();

                if (validarEspecial>0)
                {
                    var detalleE = context.DetallesVentaEspecial.Where(x => x.idDetalle == idDetalle).ToList();
                    context.DetallesVentaEspecial.RemoveRange(detalleE);

                    context.SaveChanges();


                }


                data =
                    context.DetalleVenta
                    .Where(vd => vd.idDetalle == idDetalle)
                    .AsEnumerable()
                    .Select(vd => new string[5] {
                        "",
                        vd.Producto.nombre.ToUpper(),
                        Math.Round((decimal)vd.cantidad, 2).ToString("#,0"),
                        Math.Round((decimal)vd.precioVenta, 2).ToString("$#,0.00"),
                        Math.Round((decimal)vd.subTotal, 2).ToString("$#,0.00")
                    })
                    .ToArray();

                             
                

                DetalleVenta detalle = context.DetalleVenta.Find(idDetalle);
                context.Entry(detalle).State = System.Data.Entity.EntityState.Deleted;

                if (detalle.despachada ==true)
                {
                    printer.AddSpace();
                    printer.AddSpace();
                    printer.AddSubtitle($"Producto Eliminado (Orden No. {detalle.idVenta})");
                    printer.AddSpace();
                    printer.AddTable(new string[4] { "Producto", "Cant.", "Precio", "Subtotal" }, data, true, map: new float[4] { 35f, 15f, 25f, 25f });
                    printer.AddSpace();
                    printer.AddString(DateTime.Now.ToLongDateString() +" "+ DateTime.Now.ToLongTimeString());
                    printer.AddSpace();
                    printer.Print();
                }


                Venta venta = context.Venta.Find(detalle.idVenta);
                venta.total -= detalle.subTotal;

                        

                return context.SaveChanges();


            }
        }

        [HttpPost]
        public ActionResult ModificarProductoCarrito(DetalleVenta ventaObjeto)
        {
            using (barbdEntities context = new barbdEntities())
            {
               var  validarCantidad = context.DetalleVenta.Find(ventaObjeto.idDetalle);

                if (ventaObjeto.cantidad == 0 || ventaObjeto.cantidad >= validarCantidad.cantidad)
                {
                    string error = "La cantidad debe ser mayor de cero y menor a la existente";
                    return Json(new { error });

                }


         
                var validarEspecial = context.DetallesVentaEspecial.Where(x => x.idDetalle == ventaObjeto.idDetalle).Count();

                if (validarEspecial > 0)
                {
                    

                    var detalleE = context.DetallesVentaEspecial.Where(x => x.idDetalle == ventaObjeto.idDetalle).ToList();
                    context.DetallesVentaEspecial.RemoveRange(detalleE);
                    context.SaveChanges();

                    var detalleVenta = context.DetalleVenta.Find(ventaObjeto.idDetalle);

                    Producto producto = context.Producto.Find(detalleVenta.idProducto);


                    //////////////Especiales

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

                            detalleVenta.subTotal = (float)(ventaObjeto.cantidad * producto.precioVenta);
                            detalleVenta.despachada = false; //Context.DetalleVenta.Find(detalleVenta.idDetalle).despachada == true ? "Si" : "No";
                            detalleVenta.precioVenta = producto.precioVenta;
                            detalleVenta.precioEntrada = producto.precioAlmacen;
                            detalleVenta.cantidad = ventaObjeto.cantidad;
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
                                    cantidad = int.Parse(Oproducto.idProductoEspecialCantidad) * ventaObjeto.cantidad,
                                    idCuadre = context.Cuadre.SingleOrDefault(x => x.cerrado == false).idCuadre,
                                    idDetalle = detalleVenta.idDetalle,
                                    precioEntrada = Oproducto.precioAlmacen
                                };

                                context.DetallesVentaEspecial.Add(objDetallesEspecial);
                                context.SaveChanges();

                                contador += 1;
                            };

                            DetalleVenta detalle = context.DetalleVenta.Find(ventaObjeto.idDetalle);
                            context.Entry(detalle).State = System.Data.Entity.EntityState.Deleted;

                            Venta venta_ = context.Venta.Find(detalle.idVenta);
                            venta_.total -= detalle.subTotal;

                            context.SaveChanges();


                            return Json(new
                            {
                                producto = new Producto()
                                {
                                    idProducto = producto.idProducto,
                                    nombre = producto.nombre,
                                    precioVenta = producto.precioVenta,
                                   

                                },
                                idDetalle = detalleVenta.idDetalle
                            }, JsonRequestBehavior.AllowGet);



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

                    //////////////

                    //List<Models.Inventario> queryInventario = context.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleBar").ToList();
                    //bool productoExiste = queryInventario.Any(i => i.IdProducto == producto.idProducto);
                    //int cantidadInventario = queryInventario.SingleOrDefault(i => i.IdProducto == producto.idProducto) != null ? queryInventario.SingleOrDefault(i => i.IdProducto == producto.idProducto).Cantidad : 0;
                    //if (!productoExiste || cantidadInventario < detalleVenta.cantidad)
                    //{
                    //    string error =
                    //        cantidadInventario < 1
                    //        ? "No se puede agregar el producto, no quedan en el local"
                    //        : "No se puede agregar el producto, solo quedan " + cantidadInventario + " en el local";

                    //    return Json(new { error });
                    //}

                    var detalleVenta = context.DetalleVenta.Find(ventaObjeto.idDetalle);

                    Producto producto = context.Producto.Find(detalleVenta.idProducto);

                    float subt = detalleVenta.subTotal;

                    detalleVenta.subTotal = (float)(ventaObjeto.cantidad * producto.precioVenta);
                    detalleVenta.despachada = true; //Context.DetalleVenta.Find(detalleVenta.idDetalle).despachada == true ? "Si" : "No";
                    detalleVenta.precioVenta = producto.precioVenta;
                    detalleVenta.precioEntrada = producto.precioAlmacen;
                    detalleVenta.cantidad = ventaObjeto.cantidad;

                    context.Entry(detalleVenta).State = System.Data.Entity.EntityState.Modified;

                    context.SaveChanges();

                    Venta venta = context.Venta.Find(detalleVenta.idVenta);
                    venta.total -= subt;
                    venta.total += detalleVenta.subTotal;
                    context.Entry(venta).State = System.Data.Entity.EntityState.Modified;

                    context.SaveChanges();

                    return Json(new
                    {
                        producto = new Producto()
                        {
                            idProducto = producto.idProducto,
                            nombre = producto.nombre,
                            precioVenta = producto.precioVenta,

                        },
                        idDetalle = detalleVenta.idDetalle
                    }, JsonRequestBehavior.AllowGet);

                }


                return null; // context.SaveChanges();


            }

        }


        [HttpPost]
        public ActionResult obtenerMesas()
        {
            using (var context = new barbdEntities())
            {
                List<Mesa> ListMesa = new List<Mesa>();

                var M = context.Mesa.ToList();

                foreach (var item in M)
                {
                    var Validar = context.Cliente.Count(x => x.idMesa == item.idMesa);

                    if (Validar == 0)
                    {
                        Mesa ObjMesa = new Mesa()
                        {
                            idMesa = item.idMesa,
                            descripcion = item.descripcion
                        };

                        ListMesa.Add(ObjMesa);
                    }
                }

                return PartialView("ListaMesas", ListMesa);
            }
        }

        [HttpPost]
        public ActionResult NuevaOrden(Cliente cliente)
        {

           

            using (var Context = new barbdEntities())
            {

                cliente.nMesa = "MESA-" + cliente.idMesa.ToString();

                var ObjCliente = Context.Cliente.Add(cliente);
                Context.SaveChanges();

                var ObjVenta = new Venta
                {
                    total = 0,
                    idCliente = ObjCliente.idCliente,
                    fecha = DateTime.Now,
                    IVA = Context.Impuesto.Single().Itbis.Value,
                    idUsuario = Convert.ToInt32(Session["IdUsuario"]),
                    idCuadre = Context.Cuadre.AsEnumerable().SingleOrDefault(c => !c.cerrado.GetValueOrDefault(false)).idCuadre
            };

                Context.Venta.Add(ObjVenta);
                Context.SaveChanges();

                ViewData["ListadoClienteOrdenar"] = Context.Venta.Include("Cliente").Where(x => x.idUsuario == ObjVenta.idUsuario && x.ordenCerrada == null).ToList();
                return PartialView("ListadoClientes", ViewData["ListadoClienteOrdenar"]);
            }
        }

        [HttpPost]
        public int Despachar(string waiter, int[] ids, string instrucciones)
        {
            List<string[]> data = new List<string[]>();

            using (barbdEntities context = new barbdEntities())
            {
                for (int index = 0; index < (ids?.Length ?? 0); index++)
                {
                    DetalleVenta detalleVenta = context.DetalleVenta.Find(ids[index]);
                    detalleVenta.despachada = true;
                    context.Entry(detalleVenta).State = System.Data.Entity.EntityState.Modified;

                    data.Add(new string[2] { detalleVenta.Producto.nombre.ToUpper(), detalleVenta.cantidad.ToString() });
                }

                context.SaveChanges();
            }

            Printer printer = new Printer();
            Dictionary<string, string> list = new Dictionary<string, string>();
            list.Add("Vendedor/a", waiter);
            list.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy"));
            list.Add("Hora", DateTime.Now.ToString("hh:mm:ss tt"));

            printer.AddTitle("Despachar");
            printer.AddSpace(3);
            printer.AddDescriptionList(list);
            printer.AddSpace(2);
            printer.AddTable(new string[2] { "Producto", "Cant" }, data.ToArray(),map: new float[2] {70f,30f});

            if (!string.IsNullOrWhiteSpace(instrucciones))
            {
                printer.AddSpace(2);
                printer.AddString("Instrucciones especiales:", true);
                printer.AddString(instrucciones);
            }

            printer.Print();

            return 1;
        }

        [HttpPost]
        public int Prefacturar(int id)
        {

            using (barbdEntities context1 = new barbdEntities())
            {

                int ValidarDetalles = context1.DetalleVenta.Where(x => x.idVenta == id).Count();
            
                if (ValidarDetalles ==0)
                {

                    var V = context1.Venta.Find(id);

                    V.ordenCerrada = true;
                    V.ordenFacturada = true;
                    V.Cliente.idMesa = null;

                    context1.SaveChanges();

                    Venta _venta = context1.Venta.Find(id);

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
                    context1.Factura.Add(factura);
                    context1.SaveChanges();

                    return 0;

                }


            }


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

                //var extraerMesa = context.Cliente.Find(venta.idCliente);
                //venta.Cliente.nMesa = extraerMesa.Mesa.descripcion +"-"+ extraerMesa.Mesa.idMesa.ToString();

                venta.Cliente.idMesa = null;

                result = context.SaveChanges();
                cliente = venta.Cliente.nMesa;// "xxx";//context.Cliente.Find(venta.idCliente).nombre;
                vendedor = context.Usuario.Find(venta.idUsuario).nombre;
                subtotal = (decimal)context.DetalleVenta.Where(vd => vd.idVenta == id).Sum(vd => vd.subTotal);
                itbis = context.DetalleVenta.Where(vd => vd.idVenta == id).Sum(vd => vd.precioVenta).GetValueOrDefault(0) * 0.18m;
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
            tableDetails.Add("Subtotal", (subtotal - itbis).ToString("$#,0.00"));
            tableDetails.Add("ITBIS", itbis.ToString("$#,0.00"));
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

            return 1;
        }


    }
}