
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
    
public partial class Creditos
{

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
    public Creditos()
    {

        this.Pagos = new HashSet<Pagos>();

    }


    public int id { get; set; }

    public Nullable<decimal> numFactura { get; set; }

    public int idUsuario { get; set; }

    public decimal MontoRestante { get; set; }



    public virtual Factura Factura { get; set; }

    public virtual Usuario Usuario { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]

    public virtual ICollection<Pagos> Pagos { get; set; }

}

}
