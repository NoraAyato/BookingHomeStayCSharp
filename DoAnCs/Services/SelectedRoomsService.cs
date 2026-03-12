using Microsoft.AspNetCore.Http;
using System.Text.Json;
namespace DoAnCs.Serivces
{
    public class SelectedRoomsService : ISelectedRoomsService
    {
        private const string SelectedRoomIdsKey = "SelectedRoomIds";// key của session để lưu danh sách phòng đã chọn

        public List<string> GetSelectedRoomIds(HttpContext httpContext)
        {
            var selectedRoomIdsJson = httpContext.Session.GetString(SelectedRoomIdsKey);
            return string.IsNullOrEmpty(selectedRoomIdsJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(selectedRoomIdsJson) ?? new List<string>();
        }

        public void AddSelectedRoom(HttpContext httpContext, string roomId)
        {
            if (string.IsNullOrEmpty(roomId))
                return;

            var selectedRoomIds = GetSelectedRoomIds(httpContext);
            if (!selectedRoomIds.Contains(roomId))
            {
                selectedRoomIds.Add(roomId);
                httpContext.Session.SetString(SelectedRoomIdsKey, JsonSerializer.Serialize(selectedRoomIds));
            }
        }

        public void ClearSelectedRooms(HttpContext httpContext)
        {
            httpContext.Session.Remove(SelectedRoomIdsKey);
        }
    }
}
