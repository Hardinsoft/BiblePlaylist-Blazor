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
    var audio = document.getElementById(player);
    if (audio != null) {
        var audioSource = document.getElementById(playerSource);
        if (audioSource != null) {
            audioSource.src = src;            
            audio.load();
            audio.currentTime = audioStart;
            audio.play().catch(() => {});

            audio.ontimeupdate = (ev) => {
                if (audio.currentTime > audioEnd) {
                    audio.pause();
                    if (window._audioDotNetHelpers[player]) {
                        window._audioDotNetHelpers[player].invokeMethodAsync('OnAudioEnded');
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

function isCached() {
    return window.caches.open(cacheName)
        .then(cache => cache.match(FILE_NAME))
        .then(Boolean);
}