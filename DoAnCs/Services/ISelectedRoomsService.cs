namespace DoAnCs.Serivces
{
    public interface ISelectedRoomsService
    {
        List<string> GetSelectedRoomIds(HttpContext httpContext);
        void AddSelectedRoom(HttpContext httpContext, string roomId);
        void ClearSelectedRooms(HttpContext httpContext);
    }
}
