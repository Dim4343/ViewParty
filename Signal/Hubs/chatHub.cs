using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class RoomState
{
    public string CurrentVideoUrl { get; set; }
    public string InitiatorUserName { get; set; } 
    public DateTime VideoStartTimeUtc { get; set; } 
    public bool IsPlaying { get; set; } 
    public double CurrentTimeSeconds { get; set; } 
}

public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<string, RoomState> ActiveRooms = new ConcurrentDictionary<string, RoomState>();
    private static readonly ConcurrentDictionary<string, HashSet<string>> RoomMembers = new();
    public async Task SendMessage(string roomId, string user, string message)
    {
        await Clients.Group(roomId).SendAsync("ReceiveMessage", user, message);
    }

    public async Task JoinRoom(string roomId, string userName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        if (ActiveRooms.TryGetValue(roomId, out RoomState currentRoomState))
        {
            if (!string.IsNullOrEmpty(currentRoomState.CurrentVideoUrl))
            {
                Console.WriteLine($"SignalR Hub: Отправка текущего состояния видео ({currentRoomState.CurrentVideoUrl} от {currentRoomState.InitiatorUserName}, начато в {currentRoomState.VideoStartTimeUtc}) новому пользователю {userName} в комнате {roomId}");
                await Clients.Caller.SendAsync("ReceiveNewVideo",
                                               currentRoomState.CurrentVideoUrl,
                                               currentRoomState.InitiatorUserName,
                                               currentRoomState.VideoStartTimeUtc.ToString("o"),
                                               currentRoomState.IsPlaying,
                                               currentRoomState.CurrentTimeSeconds);

                await SyncVideo(roomId, currentRoomState.CurrentVideoUrl, currentRoomState.IsPlaying, currentRoomState.CurrentTimeSeconds, currentRoomState.InitiatorUserName);
            }
        }
        else
        {
            ActiveRooms.TryAdd(roomId, new RoomState { IsPlaying = false });
        }
        if (!RoomMembers.ContainsKey(roomId))
        {
            RoomMembers[roomId] = new HashSet<string>();
        }
        RoomMembers[roomId].Add(userName);

        await Clients.Group(roomId).SendAsync("UpdateParticipants", RoomMembers[roomId]);
    }


    public async Task Send(string name, string message, string roomId)
    {
        await Clients.Group(roomId).SendAsync("addNewMessageToPage", name, message);
    }

    public async Task ChangeVideoInRoom(string roomID, string newVideoUrl, string initiatorUserName)
    {
        if (string.IsNullOrEmpty(roomID) || string.IsNullOrEmpty(newVideoUrl))
        {
            Console.WriteLine($"SignalR Hub: ChangeVideoInRoom - некорректные данные. RoomID: '{roomID}', VideoURL: '{newVideoUrl}'");
            return;
        }
        Console.WriteLine("отправка");
        Console.WriteLine($"SignalR Hub: Пользователь '{initiatorUserName}' меняет видео в комнате '{roomID}' на URL: '{newVideoUrl}'");
        DateTime videoStartTime = DateTime.UtcNow; 

        var newRoomState = new RoomState
        {
            CurrentVideoUrl = newVideoUrl,
            InitiatorUserName = initiatorUserName,
            VideoStartTimeUtc = videoStartTime,
            IsPlaying = true, 
            CurrentTimeSeconds = 0 
        };
        ActiveRooms.AddOrUpdate(roomID, newRoomState, (key, oldState) => newRoomState);

        await Clients.Group(roomID).SendAsync("ReceiveNewVideo",
                                               newVideoUrl,
                                               initiatorUserName,
                                               videoStartTime.ToString("o"),
                                               true,
                                               0);
    }

    public override Task OnConnectedAsync()
    {
        Console.WriteLine($"SignalR Hub: Client connected {Context.ConnectionId}");
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        foreach (var room in RoomMembers.Keys)
        {
            if (RoomMembers[room].Contains(Context.User.Identity.Name))
            {
                RoomMembers[room].Remove(Context.User.Identity.Name);
                await Clients.Group(room).SendAsync("UpdateParticipants", RoomMembers[room]);
            }
        }

        Console.WriteLine($"SignalR Hub: Client disconnected {Context.ConnectionId} with error: {exception?.Message}");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SyncPlayerState(string roomID, bool isPlaying, double currentTimeSeconds, string userName)
    {
        if (ActiveRooms.TryGetValue(roomID, out RoomState roomState))
        {
            roomState.IsPlaying = isPlaying;
            roomState.CurrentTimeSeconds = currentTimeSeconds;
            roomState.VideoStartTimeUtc = DateTime.UtcNow.AddSeconds(-currentTimeSeconds); 

            ActiveRooms.AddOrUpdate(roomID, roomState, (key, oldState) => roomState);

            Console.WriteLine($"SignalR Hub: {userName} синхронизирует состояние плеера в комнате {roomID}: IsPlaying={isPlaying}, Time={currentTimeSeconds}");
            await Clients.GroupExcept(roomID, Context.ConnectionId).SendAsync("UpdatePlayerState", isPlaying, currentTimeSeconds, userName);
        }
    }

    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
    }
    public async Task SyncVideo(string roomId, string videoUrl, bool isPlaying, double currentTime, string userName)
    {
        await Clients.Group(roomId).SendAsync("SyncPlayerState", roomId, isPlaying, currentTime, userName);
    }

    public async Task UpdateVideo(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId,roomId);
    }
}