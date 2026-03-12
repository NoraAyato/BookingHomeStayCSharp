namespace DoAnCs.Areas.Admin.ModelsView
{
    public class CreateUserModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string TrangThai { get; set; }
    }
}
