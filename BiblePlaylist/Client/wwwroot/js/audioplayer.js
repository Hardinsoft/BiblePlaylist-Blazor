const cacheNamePrefix = 'local-cache-';
const cacheName = cacheNamePrefix + 'biblePlaylist';
var FILE_NAME = "";

window.AudioCurrentTime = 0;

// Map for per-player DotNet helpers (for reusable AudioPlayer component)
window._audioDotNetHelpers = window._audioDotNetHelpers || {};

// New initialization for reusable component
window.initializeAudioPlayer = (playerId, dotNetHelper) => {
    window._audioDotNetHelpers[playerId] = dotNetHelper;
};

// New load function supporting multiple players and the new component
window.loadAudioFile = async (playerId, playerSourceId, src, autoplay = false, repeat = false) => {
    debugger;
    var audio = document.getElementById(playerId);
    if (audio != null) {
        var audioSource = document.getElementById(playerSourceId);
        if (audioSource != null) {
            audioSource.src = src;
            audio.load();

            if (autoplay) {
                audio.play().catch(() => { /* autoplay blocked */ });
            }

            audio.onended = (ev) => {
                var helper = window._audioDotNetHelpers[playerId];
                if (helper) {
                    helper.invokeMethodAsync('OnAudioEnded');
                } else if (window.DotNetHelper) {
                    // Fallback for legacy components
                    window.DotNetHelper.invokeMethodAsync('ChapterEnded');
                }
            };

            audio.ontimeupdate = (ev) => {
                window.AudioCurrentTime = audio.currentTime;
            };
        }
    }
};

// Legacy function kept for backward compatibility during transition
window.LoadAudioFile = async (player, playerSource, src, autoplay = 0, repeat = 0) => {
    var audio = document.getElementById(player);
    if (audio != null) {
        var audioSource = document.getElementById(playerSource);
        if (audioSource != null) {
            audioSource.src = src;
            audio.load();

            if (autoplay == 1 || autoplay === true) {
                audio.play().catch(() => {});
            }

            if (repeat == 1 || repeat === true) {
                audio.currentTime = 0;
                audio.play().catch(() => {});
            }

            audio.onended = (ev) => {
                if (window.DotNetHelper) {
                    window.DotNetHelper.invokeMethodAsync('ChapterEnded');
                }
            };

            audio.ontimeupdate = (ev) => {
                window.AudioCurrentTime = audio.currentTime;
            };
        }
    }
};

window.GetAudioCurrentTime = () => {
    return window.AudioCurrentTime;
};

window.CheckAudioFile = (player, playerSource, src, autoplay = 0) => {
    var audio = document.getElementById(player);
    if (audio != null) {
        var audioSource = document.getElementById(playerSource);
        if (audioSource != null) {
            if (!audio.src || audio.src === '') {
                audioSource.src = src;
                audio.load();
                audio.play().catch(() => {});
            }
        }

        audio.onended = (ev) => {
            if (window.DotNetHelper) {
                window.DotNetHelper.invokeMethodAsync('ChapterEnded');
            }
        };
    }
};

window.PlayAudioSegment = async (player, playerSource, src, audioStart, audioEnd) => {
    var endCount = 0;
    var audio = document.getElementById(player);
    if (audio != null) {
        var audioSource = document.getElementById(playerSource);
        if (audioSource != null) {
            audioSource.src = src;            
            audio.load();
            audio.currentTime = audioStart;
            audio.play().catch((ex) => {console.warn(`Failed to play audio segment ${src} ${audioStart} ${audioEnd}`, ex)});

            audio.ontimeupdate = (ev) => {
                if (audio.currentTime > audioEnd) {
                    var filename = window._getFilenameFromUrl(src);
                    console.log(`Audio segment ended: ${filename} ${audioStart} ${audioEnd}`);
                    endCount = endCount + 1;
                    audio.pause();
                    if (window._audioDotNetHelpers[player]) {
                        if (endCount === 1) {                           
                            window._audioDotNetHelpers[player].invokeMethodAsync('OnAudioEnded');
                        }
                    }
                }
                window.AudioCurrentTime = audio.currentTime;
            };
        }
    }
};

window.SetNetObject = (dotNetHelper) => {
    window.DotNetHelper = dotNetHelper;
};

window.NavToBook = (bookName) => {     
    var httpPath = '//' + location.host + location.pathname;    
    document.location.href = httpPath + bookName;
};

window.ScrollToTop = () => {
    window.scrollTo({ top: 0, left: 0, behavior: 'smooth' });
};

window.isCached = () => {
    return window.caches.open(cacheName)
        .then(cache => cache.match(FILE_NAME))
        .then(Boolean);
};

window._getFilenameFromUrl = (urlString) => {
    try {
        // 1. Use the native URL object to parse the string reliably.
        const url = new URL(urlString);

        // 2. The pathname contains the entire path after the domain. We need the last segment.
        // Example: /refs/heads/media/audio/43-John-01.mp3
        const pathname = url.pathname;

        // 3. Split the pathname by '/' and take the last element.
        // This elegantly handles trailing slashes or complex paths.
        const segments = pathname.split('/').filter(segment => segment.length > 0);

        if (segments.length === 0) {
            return null; // Handle root URL case
        }

        return segments[segments.length - 1];

    } catch (e) {
        console.error("Error parsing the URL:", e);
        return null;
    }
};

// === Text-to-Speech (TTS) Support for Playlist descriptions and VoiceText ===
// Uses Web Speech API (SpeechSynthesis). Speaks sequentially and waits for completion.
window.utterance;
window.speakTextAsync = (text, rate = 0.92, pitch = 1.05) => {
    return new Promise((resolve) => {
        if (!('speechSynthesis' in window) || !text || typeof text !== 'string' || !text.trim()) {
            resolve();
            return;
        }
        try {
            window.speechSynthesis.cancel();
            if(window.utterance)
                window.utterance.text = text.trim();
            else
                window.utterance = new SpeechSynthesisUtterance(text.trim());

            window.utterance.rate = Math.max(0.5, Math.min(2.0, rate));
            window.utterance.pitch = Math.max(0.5, Math.min(2.0, pitch));
            window.utterance.volume = 0.85;
            window.utterance.onend = () => resolve();
            window.utterance.onerror = (event) => {
                console.warn('TTS playback error:', event);
                resolve();
            };
            window.speechSynthesis.speak(utterance);
            
        } catch (ex) {
            console.warn('TTS exception, continuing playback:', ex);
            resolve();
        }
    });
};

window.speakSequenceAsync = async (texts, rate = 0.92) => {
    if (!texts || !Array.isArray(texts)) {
        return;
    }
    for (const t of texts) {
        if (typeof t === 'string' && t.trim()) {
            await window.speakTextAsync(t, rate);
        }
    }
};

// Pause helper for clean navigation between segments (prevents TTS + audio overlap)
window.pauseAudioPlayer = (playerId) => {
    var audio = document.getElementById(playerId);
    if (audio != null) {
        audio.pause();
    }
    if ('speechSynthesis' in window) {
        window.speechSynthesis.cancel();
    }
};

window.playAudioPlayer = (playerId) => {
    var audio = document.getElementById(playerId);
    if (audio != null) {
        audio.play();
    }
    if ('speechSynthesis' in window) {
        window.speechSynthesis.cancel();
    }
};

window.seekAudioPlayer = (playerId, time) => {
    var audio = document.getElementById(playerId);
    if (audio != null) {
        audio.currentTime = time;
    }
};

// Scroll to a specific verse element, accounting for the fixed MudAppBar header
window.scrollToVerse = (id) => {
    const el = document.getElementById(id);
    if (!el) return;

    const appBar = document.querySelector('.mud-appbar');
    const headerHeight = appBar ? appBar.offsetHeight : 64;
    const extraBuffer = 250; // extra pixels so the verse isn't right at the edge of the header

    const elementPosition = el.getBoundingClientRect().top + window.pageYOffset;
    const offsetPosition = elementPosition - headerHeight - extraBuffer;

    window.scrollTo({
        top: offsetPosition,
        behavior: 'smooth'
    });
};