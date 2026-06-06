const cacheNamePrefix = 'local-cache-';
const cacheName = cacheNamePrefix + 'biblePlaylist';
var FILE_NAME = '';

let activeAudioElement = null;

window.LoadAudioFile = async (player, playerSource, src, autoplay = 0, repeat = 0, handleFinished) => {
    
    var audio = document.getElementById(player);   
    if (audio != null) {
        var audioSource = document.getElementById(playerSource);
        if (audioSource != null) {            
                audioSource.src = src;
           
            //try {
                audio.load();
            //} catch (e) { []


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
// ... existing code ...
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

// Centralized handler for when ANY audio playback stops
const handlePlaybackEnd = (element) => {
    if (element && element.onended) {
        // Clean up the listener immediately after it fires
        element.removeEventListener('ended', handlePlaybackEnd);
    }
    // Trigger the C# callback provided by the component
    if (window.DotNetHelper && window.DotNetHelper.invokeMethodAsync) {
        window.DotNetHelper.invokeMethodAsync('HandleSegmentCompletion', element);
    }
};

// Function to play any segment, regardless of chapter context
window.PlayAudioSegment = async (player, playerSource, src, audioStart, audioEnd, segmentData, onSegmentEnd, handleFinished) => {
    
    var audio = document.getElementById(player);
    if (audio == null) return;
    
    var audioSource = document.getElementById(playerSource);
    if (audioSource == null) return;

    audioSource.src = src;

    if(audio.readyState == 0)
    audio.load();
    
    audio.currentTime = audioStart;
    audio.play();

    // 1. Setup robust event listeners BEFORE any timeout logic
    audio.removeEventListener('ended', handlePlaybackEnd); // Prevent duplicates
    audio.addEventListener('ended', handlePlaybackEnd);

    // 2. Store the necessary cleanup/callback data on the element itself for reliable cleanup
    audio.dataset.segmentEndHandler = onSegmentEnd;
    audio.dataset.segmentDuration = (audioEnd - audioStart) * 1000;


    // 3. The dedicated end handler that executes when the browser signals 'ended'
    const segmentEndHandler = (ev) => {
        // IMPORTANT: The handlePlaybackEnd function attached via event listener will handle cleanup.
        // We just need to ensure the callback is fired.
    };

    // 4. Use a timeout as a HARD FAILSAFE stop, which triggers the same cleanup logic
    setTimeout(() => {
        if (audio && audio.pause) {
            audio.pause();
            audio.currentTime = 0; // Reset position
        }
        // Manually trigger the end handler logic
        handlePlaybackEnd(audio);
    }, (audioEnd - audioStart) * 1000 + 100); // Add buffer time
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

const audioEl = document.getElementById('my-audio-player');

const handleFinished = (element) => {
    console.log("Playback sequence finished successfully.");
    // Perform actions here (e.g., move to next slide)
};

//window.SetNetObject(window.DotNetHelper);



