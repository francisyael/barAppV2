
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


namespace barApp
{

using System;
    using System.Collections.Generic;
    
public partial class ProductoEliminado
{

    public int IdProductoEliminado { get; set; }

    public string NoOrden { get; set; }

    public string IdProducto { get; set; }

    public Nullable<int> Cantidad { get; set; }

    public Nullable<decimal> Precio { get; set; }

    public Nullable<int> IdCuadre { get; set; }



    public virtual Cuadre Cuadre { get; set; }

    public virtual Producto Producto { get; set; }

}

}
