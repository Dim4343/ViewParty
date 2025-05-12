/***

Файл с моими попытками.....


using Microsoft.Scripting.Hosting.Shell;
using static Community.CsharpSqlite.Sqlite3;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System;
using ViewParty.Video;

document.addEventListener("DOMContentLoaded", () => {
const PLAYER_HOST_ID = "player-host";
const VK_API_ID = "YOUR_VK_API_ID";

if (VK_API_ID === "YOUR_VK_API_ID" || !VK_API_ID)
{
    console.warn("ВНИМАНИЕ: VK_API_ID не установлен! VK плеер не будет работать.");
    alert("Пожалуйста, установите VK_API_ID в script.js для работы VK плеера.");
}
else
{
    try
    {
        VK.init({ apiId: VK_API_ID });
                } catch (e) {
    console.error("Ошибка инициализации VK API:", e);
}
            }


            // --- Глобальные переменные ---
            let timeTrackInterval = null;
let currentPlayerInstance = null;
let currentPlayerType = null; // "youtube", "vk", "rutube"
let isYouTubeApiReady = false;

// --- Инициализация API ---
window.onYouTubeIframeAPIReady = function() {
    console.log("YouTube IFrame API Ready");
    isYouTubeApiReady = true;
    // Если ссылка уже была введена до готовности API, можно попробовать загрузить
    const initialLink = document.getElementById("inputLink").value.trim();
    if (initialLink && !currentPlayerInstance)
    {
        // handleLinkMessage(initialLink); // Раскомментируйте, если нужна автозагрузка при старте
    }
};

const playerFactory = {
                "youtube": (videoId, hostElementId) => {
                    if (!isYouTubeApiReady)
                    {
                        console.warn("YouTube API еще не готово. Попробуйте позже.");
                        alert("YouTube API еще не загрузилось. Пожалуйста, подождите и попробуйте снова.");
                        return null;
                    }
                    return new YT.Player(hostElementId, {
                        // Размеры будут управляться CSS через .player-wrapper
                        videoId: videoId, // Чистый ID видео
                        playerVars: {
                        'playsinline': 1,
                            'enablejsapi': 1,
                            'autoplay': 1, // 1 для автовоспроизведения (может быть заблокировано браузером)
                            'controls': 1,
                            'origin': window.location.origin // Важно для enablejsapi
                        },
                        events:
                    {
                        'onReady': (event) => onPlayerReady("youtube", event.target),
                            'onStateChange': (event) => onPlayerStateChange("youtube", event.data, event.target),
                            'onError': (event) => onPlayerError("youtube", event.data)
                        },
                    });
                },
                "vk": (params, hostElementId) => {
                    if (!VK || !VK.Widgets || !VK.Widgets.Player)
                    {
                        console.error("VK SDK не загружен или не инициализирован.");
                        alert("Ошибка с VK плеером: SDK не доступен.");
                        return null;
                    }
                    if (!params || !params.oid || !params.id || !params.hash) {
                        console.error("Недостаточно параметров для VK плеера:", params);
                        alert("Не удалось извлечь все необходимые параметры для VK видео.");
                        return null;
                    }
                    // VK.Widgets.Player сам создает iframe/div внутри hostElementId
                    // Размеры будут управляться CSS через .player-wrapper
                    try
                    {
                        // VK.Widgets.Player создает плеер, но для управления API нужен VK.Player
                        VK.Widgets.Player(hostElementId, params.oid, params.id, params.hash, {
                        // width и height будут из CSS
                        autoplay: 1, // 1 для автовоспроизведения
                            events:
                            {
                                'onReady': (vkPlayerApiFromEvent) => {
                                    // VK API может передавать плеер в onReady, но это не всегда документировано
                                    // Более надежно получить его через VK.Player(hostElementId)
                                    const vkPlayer = VK.Player(hostElementId);
                                    onPlayerReady("vk", vkPlayer);
                                },
                                'onStateChanged': (state) => {
                                    const vkPlayer = VK.Player(hostElementId);
                                    onPlayerStateChange("vk", state, vkPlayer);
                                },
                                'onError': (errorData) => onPlayerError("vk", errorData)
                            }
                        });
                        // Попытка получить объект плеера для возврата, хотя он может быть не сразу доступен
                        return VK.Player(hostElementId);
                    }
                    catch (e)
                    {
                        console.error("Ошибка инициализации VK плеера:", e);
                        alert("Произошла ошибка при попытке загрузить VK видео.");
                        return null;
                    }
                },
                "rutube": (videoId, hostElementId) => {
                    console.warn("Rutube плеер еще не реализован.");
                    const hostElement = document.getElementById(hostElementId);
                    if (hostElement)
                    {
                        hostElement.innerHTML = `< p style = "color:white; text-align:center; padding-top: 40%;" > Rutube плеер пока не поддерживается.</ p >`;
                    }
                    // Возвращаем заглушку с интерфейсом для консистентности
                    return {
                    destroy: () => { if (hostElement) hostElement.innerHTML = ''; },
                        // ... другие mock-методы если нужны
                    };
                },
            };

document.getElementById("sendLink").addEventListener("click", () => {
    const link = document.getElementById("inputLink").value.trim();
    if (link)
    {
        handleLinkMessage(link);
    }
    else
    {
        alert("Пожалуйста, введите ссылку на видео.");
    }
});

function handleLinkMessage(link)
{
    const playerHostElement = document.getElementById(PLAYER_HOST_ID);
    if (!playerHostElement)
    {
        console.error(`Контейнер для плеера с ID "${PLAYER_HOST_ID}" не найден.`);
        return;
    }

    const videoInfo = getVideoInfoFromLink(link);

    if (!videoInfo || !videoInfo.type || !videoInfo.id)
    {
        console.error("Не удалось распознать ссылку или извлечь ID видео:", link);
        alert("Не удалось распознать ссылку. Поддерживаются YouTube и VK видео (проверьте формат ссылки).");
        playerHostElement.innerHTML = '<p style="color:white; text-align:center; padding-top: 40%;">Неверная ссылка или тип видео не поддерживается.</p>';
        return;
    }

    destroyCurrentPlayer(); // Уничтожаем предыдущий плеер и очищаем контейнер

    if (playerFactory[videoInfo.type])
    {
        if (videoInfo.type === 'youtube' && !isYouTubeApiReady)
        {
            alert("YouTube API еще не загрузилось. Пожалуйста, подождите немного и попробуйте снова вставить ссылку.");
            console.warn("Попытка загрузить YouTube видео до готовности API.");
            return;
        }

        currentPlayerInstance = playerFactory[videoInfo.type](videoInfo.id, PLAYER_HOST_ID); // videoInfo.id для YT - строка, для VK - объект
        currentPlayerType = videoInfo.type;

        if (!currentPlayerInstance && videoInfo.type === "vk")
        {
            // VK.Widgets.Player не возвращает экземпляр сразу, он придет в onReady
            console.log("VK плеер инициализируется, ожидаем onReady...");
        }
        else if (!currentPlayerInstance && videoInfo.type !== "rutube")
        { // Rutube возвращает заглушку
            playerHostElement.innerHTML = `< p style = "color:white; text-align:center; padding-top: 40%;" > Не удалось загрузить плеер ${ videoInfo.type}.</ p >`;
        }

    }
    else
    {
        console.warn("Плеер для данного типа не найден в playerFactory:", videoInfo.type);
        alert(`Поддержка для ${ videoInfo.type}
        еще не реализована.`);
        playerHostElement.innerHTML = `< p style = "color:white; text-align:center; padding-top: 40%;" > Поддержка для ${ videoInfo.type}
        еще не реализована.</ p >`;
    }
}

function destroyCurrentPlayer()
{
    stopTrackingTime();
    if (currentPlayerInstance)
    {
        if (typeof currentPlayerInstance.destroy === 'function')
        {
            try
            {
                currentPlayerInstance.destroy();
            }
            catch (e)
            {
                console.error("Ошибка при уничтожении плеера:", currentPlayerType, e);
            }
        }
        else if (currentPlayerType === "vk" && VK && VK.Player)
        {
            // Для VK виджета может не быть явного destroy(), очистка контейнера достаточна
        }
    }
    const playerHostElement = document.getElementById(PLAYER_HOST_ID);
    if (playerHostElement)
    {
        playerHostElement.innerHTML = ''; // Очищаем контейнер
    }
    currentPlayerInstance = null;
    currentPlayerType = null;
    document.getElementById("video-info").textContent = "Информация о видео будет здесь...";
}

function getVideoInfoFromLink(link)
{
    let match;
    // YouTube
    match = link.match(/ (?: https ?:\/\/) ? (?: www\.)?(?: youtube\.com\/ (?: watch\?v =| embed\/| shorts\/)| youtu\.be\/)([a - zA - Z0 - 9_ -]{ 11})/);
    if (match && match[1])
    {
        return { type: "youtube", id: match[1] };
    }
    // VK Video
    // 1. video_ext.php?oid=GROUP_ID&id=VIDEO_ID&hash=HASH
    match = link.match(/ vk\.com\/ video_ext\.php\?oid = (-?\d +) & id = (\d +)&hash = ([a - zA - Z0 - 9] +) /);
    if (match)
    {
        return { type: "vk", id: { oid: match[1], id: match[2], hash: match[3] } };
    }
    // 2. vk.com/videoOWNERID_VIDEOID (OWNERID может быть с минусом для групп)
    match = link.match(/ vk\.com\/ video(-?\d +)_(\d +) /);
    if (match)
    {
        // Для этого типа ссылки нужен hash, который не всегда доступен.
        // VK.Widgets.Player может не работать без hash или для некоторых приватных видео.
        // Это упрощение, в идеале нужен запрос к VK API для получения hash.
        console.warn("Для ссылок VK вида /videoOWNER_ID_VIDEO_ID может потребоваться hash, который не извлекается из URL. Плеер может не работать.");
        // Попробуем передать без hash, но это не гарантирует работу
        return { type: "vk", id: { oid: match[1], id: match[2], hash: null } };
    }
    // Rutube (пример, может потребовать уточнения)
    match = link.match(/ rutube\.ru\/ (?: video | play\/ embed)\/ ([a - zA - Z0 - 9] +) /);
    if (match && match[1])
    {
        return { type: "rutube", id: match[1] };
    }
    return null;
}

function onPlayerReady(type, playerApi)
{
    console.log(`${ type}
    player ready.`);
    if (type === "vk" && playerApi && typeof playerApi.play === 'function')
    {
        // Если это VK и мы получили API, сохраняем его
        currentPlayerInstance = playerApi;
    }
    // Для YouTube event.target (переданный как playerApi) уже является экземпляром
    // Для VK может потребоваться получить экземпляр здесь, если он не был доступен ранее

    if (currentPlayerInstance && typeof currentPlayerInstance.play === 'function' && (type === "youtube" || type === "vk"))
    {
        try
        {
            if (type === 'youtube') currentPlayerInstance.playVideo(); // Автостарт для YT
            else if (type === 'vk') currentPlayerInstance.play(); // Автостарт для VK
        }
        catch (e)
        {
            console.warn("Ошибка автостарта плеера", e);
        }
    }
    startTrackingTime();
    updateVideoInfoDisplay();
}

function onPlayerStateChange(type, state, playerApi)
{
    let unifiedState = -1; // YT.PlayerState.UNSTARTED по умолчанию

    if (type === "youtube")
    {
        unifiedState = state; // state уже число от YT API
    }
    else if (type === "vk")
    {
        // playerApi здесь может быть полезен, если глобальный currentPlayerInstance еще не установлен для VK
        if (playerApi && typeof playerApi.play === 'function' && !currentPlayerInstance)
        {
            currentPlayerInstance = playerApi;
        }
        // VK States: 'playing', 'paused', 'buffering', 'ended', 'api_unavailable', 'loading', 'loaded'
        switch (String(state).toLowerCase())
        { // Приводим к строке и нижнему регистру на всякий случай
            case 'playing': unifiedState = YT.PlayerState.PLAYING; break;
            case 'paused': unifiedState = YT.PlayerState.PAUSED; break;
            case 'buffering': unifiedState = YT.PlayerState.BUFFERING; break;
            case 'ended': unifiedState = YT.PlayerState.ENDED; break;
            // 'loading', 'loaded', 'api_unavailable' можно считать как UNSTARTED или BUFFERING
            default: unifiedState = YT.PlayerState.UNSTARTED;
        }
    }
    // console.log(`${type} state change: ${state} -> unified: ${unifiedState}`);
    updateVideoInfoDisplay(); // Обновляем инфо при смене состояния

    if (unifiedState === YT.PlayerState.PLAYING)
    {
        startTrackingTime();
    }
    else if (unifiedState === YT.PlayerState.PAUSED || unifiedState === YT.PlayerState.ENDED)
    {
        stopTrackingTime(); // Можно остановить, если не играет, чтобы не тратить ресурсы
    }
}

function onPlayerError(type, errorData)
{
    console.error(`${ type}
    player error:`, errorData);
const errorMsg = (type === 'youtube') ? `Код ошибки YT: ${ errorData}` : `Ошибка VK: ${ JSON.stringify(errorData)}`;
alert(`Произошла ошибка с плеером ${ type}. ${ errorMsg}`);
document.getElementById("video-info").textContent = `Ошибка плеера ${type}.`;
stopTrackingTime();
            }

            function updateVideoInfoDisplay()
{
    if (!currentPlayerInstance || !currentPlayerType || currentPlayerType === "rutube")
    {
        // Для Rutube (заглушки) или если плеера нет, не обновляем
        return;
    }

    let currentTime = 0, duration = 0, playerState = -1;

    try
    {
        switch (currentPlayerType)
        {
            case "youtube":
                if (typeof currentPlayerInstance.getCurrentTime === 'function') currentTime = currentPlayerInstance.getCurrentTime();
                if (typeof currentPlayerInstance.getDuration === 'function') duration = currentPlayerInstance.getDuration();
                if (typeof currentPlayerInstance.getPlayerState === 'function') playerState = currentPlayerInstance.getPlayerState();
                break;
            case "vk":
                // VK API для времени/длительности может быть другим или требовать асинхронных вызовов
                // Это предположение, что методы существуют синхронно:
                if (currentPlayerInstance && typeof currentPlayerInstance.getCurrentTime === 'function')
                {
                    currentTime = currentPlayerInstance.getCurrentTime();
                }
                if (currentPlayerInstance && typeof currentPlayerInstance.getDuration === 'function')
                {
                    duration = currentPlayerInstance.getDuration();
                }
                if (currentPlayerInstance && typeof currentPlayerInstance.getState === 'function')
                {
                    const vkState = String(currentPlayerInstance.getState()).toLowerCase();
                    switch (vkState)
                    {
                        case 'playing': playerState = YT.PlayerState.PLAYING; break;
                        case 'paused': playerState = YT.PlayerState.PAUSED; break;
                        case 'buffering': playerState = YT.PlayerState.BUFFERING; break;
                        case 'ended': playerState = YT.PlayerState.ENDED; break;
                        default: playerState = -1;
                    }
                }
                break;
        }
    }
    catch (e)
    {
        console.warn("Ошибка при получении информации о видео:", currentPlayerType, e);
    }

    const infoDiv = document.getElementById("video-info");
    if (infoDiv)
    {
        infoDiv.innerHTML = `
                        < strong > Плеер:</ strong > ${ currentPlayerType.toUpperCase()}< br >
                        < strong > Состояние:</ strong > ${ playerStateToString(playerState)} (${ playerState})< br >
                        < strong > Время:</ strong > ${ formatTime(currentTime)} / ${ formatTime(duration)}
                    `;
                }
            }

            function playerStateToString(state)
{
    // Используем константы YT.PlayerState для единообразия
    if (typeof YT !== 'undefined' && typeof YT.PlayerState !== 'undefined')
    {
        for (const key in YT.PlayerState) {
            if (YT.PlayerState[key] === state)
            {
                return key;
            }
        }
    }
    // Если YT не загружен или состояние не найдено
    switch (state)
    {
        case -1: return "UNSTARTED";
        case 0: return "ENDED";
        case 1: return "PLAYING";
        case 2: return "PAUSED";
        case 3: return "BUFFERING";
        case 5: return "CUED";
        default: return "UNKNOWN";
    }
}

function formatTime(timeInSeconds)
{
    if (isNaN(timeInSeconds) || timeInSeconds < 0) return "0:00";
    const minutes = Math.floor(timeInSeconds / 60);
    const seconds = Math.floor(timeInSeconds % 60).toString().padStart(2, '0');
    return `${ minutes}:${ seconds}`;
            }

            function startTrackingTime()
{
    stopTrackingTime(); // Очищаем предыдущий интервал, если есть
    timeTrackInterval = setInterval(updateVideoInfoDisplay, 1000);
}

function stopTrackingTime()
{
    if (timeTrackInterval)
    {
        clearInterval(timeTrackInterval);
        timeTrackInterval = null;
    }
}
        });
*/