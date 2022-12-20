
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
    
public partial class Venta
{

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
    public Venta()
    {

        this.DetalleVenta = new HashSet<DetalleVenta>();

        this.Factura = new HashSet<Factura>();

        this.DetallesVentaEspecial = new HashSet<DetallesVentaEspecial>();

    }


    public decimal idVenta { get; set; }

    public float total { get; set; }

    public decimal idCliente { get; set; }

    public Nullable<System.DateTime> fecha { get; set; }

    public decimal IVA { get; set; }

    public Nullable<bool> ordenCerrada { get; set; }

    public Nullable<int> idUsuario { get; set; }

    public Nullable<bool> ordenFacturada { get; set; }

    public Nullable<int> idCuadre { get; set; }



    public virtual Cliente Cliente { get; set; }

    public virtual Cuadre Cuadre { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]

    public virtual ICollection<DetalleVenta> DetalleVenta { get; set; }

    public virtual Usuario Usuario { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]

    public virtual ICollection<Factura> Factura { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]

    public virtual ICollection<DetallesVentaEspecial> DetallesVentaEspecial { get; set; }

}

}
