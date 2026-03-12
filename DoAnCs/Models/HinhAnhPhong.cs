    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace DoAnCs.Models
    {
        public class HinhAnhPhong
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }

            [Required]
            [ForeignKey("Phong")]
            public string MaPhong { get; set; }

            [Required]
            [StringLength(500)]
            public string UrlAnh { get; set; }

            [StringLength(100)]
            public string MoTa { get; set; }

            public bool LaAnhChinh { get; set; }

            public virtual Phong Phong { get; set; }
        }
}