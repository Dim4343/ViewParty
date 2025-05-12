document.addEventListener('DOMContentLoaded', () => {
    const videoUrlInput = document.getElementById('inputLink');
    const processButton = document.getElementById('sendLink');
    const player = document.getElementById('videoFrame');

    processButton.addEventListener('click', async () => {
        const videoUrl = videoUrlInput.value.trim();
        if (!videoUrl) {
            return;
        }

        processButton.disabled = true; 

        try {
            const response = await fetch('https://localhost:7207/download-video', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ videoUrl: videoUrl}),
            });

            const result = await response.json();

            if (result.success && result.videoUrl) {
                player.src = result.videoUrl;
                player.load();
                player.play().catch(err => {
                    console.warn("Не удалось автоматически воспроизвести видео:", err);
                });
            } else {
                player.src = ''; 
            }
        } catch (error) {
            console.error('Ошибка при запросе к серверу:', error);
        } finally {
            processButton.disabled = false; 
        }
    });

    player.addEventListener('error', (e) => {
        let errorMsg = 'Не удалось загрузить или воспроизвести видео.';
        if (player.error) {
            switch (player.error.code) {
                case player.error.MEDIA_ERR_ABORTED:
                    errorMsg = 'Загрузка видео прервана.';
                    break;
                case player.error.MEDIA_ERR_NETWORK:
                    errorMsg = 'Проблема с сетью при загрузке видео.';
                    break;
                case player.error.MEDIA_ERR_DECODE:
                    errorMsg = 'Видео не может быть декодировано или формат не поддерживается.';
                    break;
                case player.error.MEDIA_ERR_SRC_NOT_SUPPORTED:
                    errorMsg = 'Формат видео не поддерживается или источник недоступен.';
                    break;
                default:
                    errorMsg = 'Произошла неизвестная ошибка плеера.';
            }
        }
        console.error('Ошибка плеера:', e, player.error);
    });
});