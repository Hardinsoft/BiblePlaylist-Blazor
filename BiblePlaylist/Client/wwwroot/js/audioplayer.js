const cacheNamePrefix = 'local-cache-';
const cacheName = cacheNamePrefix + 'biblePlaylist';
var FILE_NAME = "";

window.AudioCurrentTime = 0;

window.LoadAudioFile = async (player, playerSource, src, autoplay = 0, repeat = 0) => {
    
    var audio = document.getElementById(player);   
    if (audio != null) {
        var audioSource = document.getElementById(playerSource);
        if (audioSource != null) {            
                audioSource.src = src
           
            //try {
                audio.load();
            //} catch (e) { }


            if (autoplay == 1) {
               audio.play();                
            }

            if (repeat == 1) {
                audio.currentTime = 0;
                audio.play();
            }

            audio.onended = (ev) => {
                window.DotNetHelper.invokeMethodAsync('ChapterEnded');                
            };

            audio.ontimeupdate = (ev) => {                
                window.AudioCurrentTime = audio.currentTime;
            };
        }
    }
}

window.GetAudioCurrentTime = () => {
    return window.AudioCurrentTime;
}

window.CheckAudioFile = (player, playerSource, src, autoplay = 0) => {    
    var audio = document.getElementById(player);
    if (audio != null) {
        var audioSource = document.getElementById(playerSource);
        if (audioSource != null) {
            if (audio.src == null) {
                audioSource.src = src;
                audio.load();
                audio.play();
            }

        }

        audio.onended = (ev) => { 
            window.DotNetHelper.invokeMethodAsync('ChapterEnded');
        };
    }
}

window.PlayAudioSegment = async (player, playerSource, src, audioStart, audioEnd) => {

    var audio = document.getElementById(player);
    if (audio != null) {
        var audioSource = document.getElementById(playerSource);
        if (audioSource != null) {
            audioSource.src = src

            if(audio.readyState == 0)
                audio.load();
            
            audio.currentTime = audioStart;

            audio.play();

            audio.ontimeupdate = (ev) => {
                if (audio.currentTime > audioEnd) { 
                    audio.pause();
                    audioEnd = audio.duration;
                }
                window.AudioCurrentTime = audio.currentTime;
            };
        }
    }
}

window.SetNetObject = (dotNetHelper) => {
    window.DotNetHelper = dotNetHelper;
}

window.NavToBook = (bookName) => {     
    var httpPath = '//' + location.host + location.pathname;    
    document.location.href = httpPath + bookName;
}

window.ScrollToTop = () => {
    window.scrollTo({ top: 0, left: 0, behavior: 'smooth' });
}

function isCached() {
    return window.caches.open(cacheName)
        .then(cache => cache.match(FILE_NAME))
        .then(Boolean);
}

//isCached().then(value => {
//    if (value) {
//        // Cached
//        console.log("mp3 is in cache");
//    }
//    else {
//        // Not cached. Add it
//        console.log("Adding mp3 to cache");
//        window.caches.open(cacheName)
//            .then(cache => cache.add(FILE_NAME))
//            .then(() => {
//                console.log('added cached file');
//                // Notify the Blazor component
//                //dotNetReference.invokeMethodAsync("AddedToCache", FILE_NAME);
//            })
//    };
//});

//const cache = await window.caches.open(cacheName);
//console.log("Open cache");
//cachedResponse = await cache.match(src);
//if (cachedResponse) {
//    console.log("Found cached response");
//    audioSource.src = cachedResponse;
//}
//else {
//    console.log("Cache not found, load from online resource");
//}