﻿

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
using System.Data.Entity;
using System.Data.Entity.Infrastructure;


public partial class barbdEntities : DbContext
{
    public barbdEntities()
        : base("name=barbdEntities")
    {

    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        throw new UnintentionalCodeFirstException();
    }


    public virtual DbSet<Categoria> Categoria { get; set; }

    public virtual DbSet<Cliente> Cliente { get; set; }

    public virtual DbSet<Cuadre> Cuadre { get; set; }

    public virtual DbSet<Impuesto> Impuesto { get; set; }

    public virtual DbSet<Inventario> Inventario { get; set; }

    public virtual DbSet<ModoPago> ModoPago { get; set; }

    public virtual DbSet<Producto> Producto { get; set; }

    public virtual DbSet<Roles> Roles { get; set; }

    public virtual DbSet<Suplidor> Suplidor { get; set; }

    public virtual DbSet<TipoCategoria> TipoCategoria { get; set; }

    public virtual DbSet<Usuario> Usuario { get; set; }

    public virtual DbSet<Gastos> Gastos { get; set; }

    public virtual DbSet<InventarioBar> InventarioBar { get; set; }

    public virtual DbSet<Mesa> Mesa { get; set; }

    public virtual DbSet<DetalleVenta> DetalleVenta { get; set; }

    public virtual DbSet<Venta> Venta { get; set; }

    public virtual DbSet<Configuraciones> Configuraciones { get; set; }

    public virtual DbSet<Factura> Factura { get; set; }

    public virtual DbSet<Creditos> Creditos { get; set; }

    public virtual DbSet<Pagos> Pagos { get; set; }

    public virtual DbSet<DetallesVentaEspecial> DetallesVentaEspecial { get; set; }

    public virtual DbSet<ComprobanteFiscal> ComprobanteFiscal { get; set; }

    public virtual DbSet<Impresoras> Impresoras { get; set; }

    public virtual DbSet<ProductoEliminado> ProductoEliminado { get; set; }

}

}

