const cacheNamePrefix = 'local-cache-';
const cacheName = cacheNamePrefix + 'biblePlaylist';
var FILE_NAME = "";

window.AudioCurrentTime = 0;

// Modified LoadAudioFile function to accept a handleFinished callback
window.LoadAudioFile = async (player, playerSource, src, autoplay = 0, repeat = 0, handleFinished) => {
    
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

// Modified PlayAudioSegment function to accept a handleFinished callback
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

    // Use setTimeout for controlled stopping
    const duration = audioEnd - audioStart;

    // Set up the end handler
    const segmentEndHandler = () => {
        audio.removeEventListener('timeupdate', segmentTimeUpdateHandler);
        audio.removeEventListener('ended', segmentEndHandler);
        
        // Signal the Blazor component that the segment has ended
        //if (onSegmentEnd) {
        //    onSegmentEnd(audio.currentTime, audio.duration);
        //}
    };

    audio.addEventListener('ended', segmentEndHandler);
    
    // Use a timeout to force stop and trigger the end handler
    setTimeout(() => {
        if (audio && audio.pause) {
            audio.pause();
            audio.currentTime = 0; // Reset position
        }
        // Manually trigger the end handler logic
       // if (onSegmentEnd) {
       //     onSegmentEnd(audio.currentTime, audio.duration);
       // }
    }, duration * 1000);

    // Event listener for time updates (used for highlighting)
    function segmentTimeUpdateHandler() {
        // This function will be called by the Blazor component to handle highlighting
        //window.DotNetHelper.invokeMethodAsync('UpdateSegmentHighlight', audio.currentTime);
    }
    audio.addEventListener('timeupdate', segmentTimeUpdateHandler);

    // Call the provided handleFinished callback when playback finishes
    audio.onended = () => {
        handleFinished(audio);
    };
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

//window.PlayAudioSegment(audioEl, '', '', 0, 10, null, null, handleFinished);
