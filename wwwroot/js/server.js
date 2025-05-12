const express = require('express');
const { exec } = require('youtube-dl-exec'); 
const path = require('path');
const fs = require('fs');
const { v4: uuidv4 } = require('uuid');
const cors = require('cors'); 

const app = express();
const port = 3000;

const videosDir = path.join(__dirname, 'public', 'videos');
if (!fs.existsSync(videosDir)) {
    fs.mkdirSync(videosDir, { recursive: true });
}

app.use(express.json());
app.use(express.urlencoded({ extended: true }));

app.use(cors({
    origin : "*"
}));

app.use(express.static(path.join(__dirname, 'public')));

app.post('/download-video', async (req, res) => {
    const videoUrlFromUser = req.body.videoUrl;
    if (!videoUrlFromUser) {
        return res.status(400).json({ success: false, message: 'URL видео не предоставлен.' });
    }

    const uniqueId = uuidv4();
    const finalOutputFilename = `${uniqueId}.mp4`;
    const finalOutputFilePath = path.join(videosDir, finalOutputFilename);

    console.log(`Попытка скачать и объединить: ${videoUrlFromUser} в ${finalOutputFilePath}`);

    try {
        const ytDlpOptions = {
            output: finalOutputFilePath,
            format: 'bestvideo+bestaudio',
            mergeOutputFormat: 'mp4'
        };

        console.log('Запуск yt-dlp с опциями:', ytDlpOptions);
        await exec(videoUrlFromUser, ytDlpOptions);
        console.log(finalOutputFilename)

        if (fs.existsSync(finalOutputFilePath)) {
            const serverVideoUrl = `/videos/${finalOutputFilename}`;

            console.log(`Видео успешно скачано и объединено: ${finalOutputFilename}, доступно по URL: ${serverVideoUrl}`);
            res.json({ success: true, videoUrl: serverVideoUrl, message: 'Видео успешно обработано и объединено!' });
        } else {
            console.error(`Не удалось найти итоговый файл ${finalOutputFilename} после выполнения yt-dlp.`);
            let foundFile = null;
            const filesInDir = fs.readdirSync(videosDir);
            for (const file of filesInDir) {
                if (file.startsWith(uniqueId) && file.endsWith('.mp4')) {
                    foundFile = file;
                    break;
                }
            }
            if (foundFile) {
                const serverVideoUrl = `/videos/${foundFile}`;
                console.warn(`Итоговый файл ${finalOutputFilename} не найден, но найден ${foundFile}. Используем его.`);
                res.json({ success: true, videoUrl: serverVideoUrl, message: 'Видео успешно обработано!' });
            } else {
                console.error('Скачанный файл не найден в директории:', videosDir, 'Файлы:', filesInDir);
                return res.status(500).json({ success: false, message: 'Ошибка сервера: не найден скачанный и объединенный файл.' });
            }
        }

    } catch (error) {
        const errorMessage = error.stderr || error.message || 'Неизвестная ошибка yt-dlp/ffmpeg';
        console.error('Ошибка при скачивании/объединении видео:', errorMessage, error);
        res.status(500).json({ success: false, message: `Не удалось скачать или объединить видео. Ошибка: ${errorMessage}` });
    }
});

app.get('/', (req, res) => {
    res.sendFile(path.join(__dirname, 'public', 'index.html'));
});

app.listen(port,"0.0.0.0", () => {
    console.log(`Сервер запущен на http://localhost:${port}`);
});
