using System;
using System.Threading.Tasks;

namespace BiblePlaylist.Client.Shared;

/// <summary>
/// Shared playback helper that manages the 300ms position-poll timer,
/// current verse resolution, and scroll-to-verse calls.
///
/// Does NOT own the AudioPlayer ref or touch the DOM — the consuming
/// page handles audio setup and highlight rendering.
///
/// Pattern: Configure delegates → Start() after audio setup →
/// page reads CurrentVerseId for highlight → Stop() on track end/cancel.
/// </summary>
public class SegmentPlaybackHelper
{
    private System.Timers.Timer? _timer;
    private decimal _currentPlaybackTime;
    private string? _currentVerseId;
    private string? _lastScrolledVerseId;

    private Func<Task<decimal>>? _getAudioTime;
    private Func<decimal, string?>? _resolveCurrentVerseId;
    private Func<string, Task>? _scrollToVerse;

    /// <summary>
    /// Invoked after each position update so the consuming page can
    /// re-render (StateHasChanged / drop-container refresh).
    /// </summary>
    public Func<Task>? OnPositionUpdated { get; set; }

    public string? CurrentVerseId => _currentVerseId;
    public decimal PlaybackTime => _currentPlaybackTime;

    /// <summary>
    /// Configure the delegates the helper uses for polling, verse resolution, and scrolling.
    /// Call before Start().
    /// </summary>
    public void Configure(
        Func<Task<decimal>> getAudioTime,
        Func<decimal, string?> resolveCurrentVerseId,
        Func<string, Task> scrollToVerse)
    {
        _getAudioTime = getAudioTime;
        _resolveCurrentVerseId = resolveCurrentVerseId;
        _scrollToVerse = scrollToVerse;
    }

    /// <summary>
    /// Start the 300ms position-poll timer. Call after the audio player is set up.
    /// </summary>
    public void Start()
    {
        Stop();
        _timer = new System.Timers.Timer(300);
        _timer.Elapsed += async (_, _) => await UpdatePosition();
        _timer.AutoReset = true;
        _timer.Start();
    }

    /// <summary>
    /// Stop the timer. Call on track end or when playback is cancelled.
    /// </summary>
    public void Stop()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Dispose();
            _timer = null;
        }
    }

    /// <summary>
    /// Poll the current audio time, resolve the current verse, and notify the page.
    /// Called by the timer every 300ms.
    /// </summary>
    public async Task UpdatePosition()
    {
        try
        {
            if (_getAudioTime == null || _resolveCurrentVerseId == null)
                return;

            decimal time = await _getAudioTime();
            if (Math.Abs(time - _currentPlaybackTime) <= 0.05m)
                return;

            _currentPlaybackTime = time;

            string? verseId = _resolveCurrentVerseId(time);
            _currentVerseId = verseId;

            if (OnPositionUpdated != null)
                await OnPositionUpdated();

            if (!string.IsNullOrEmpty(verseId) && verseId != _lastScrolledVerseId)
            {
                _lastScrolledVerseId = verseId;
                if (_scrollToVerse != null)
                    await _scrollToVerse(verseId);
            }
        }
        catch
        {
            // ignore transient JS interop errors
        }
    }

    /// <summary>
    /// Seek to a specific time. Resets verse tracking so the next poll picks up the correct verse.
    /// </summary>
    public void Seek(decimal time)
    {
        _currentPlaybackTime = time;
        _lastScrolledVerseId = null;
        _currentVerseId = null;
        OnPositionUpdated?.Invoke();
    }

    /// <summary>
    /// Reset playback state (time, verse, scroll). Call when starting a new segment.
    /// </summary>
    public void Reset()
    {
        _currentPlaybackTime = 0;
        _currentVerseId = null;
        _lastScrolledVerseId = null;
        OnPositionUpdated?.Invoke();
    }
}
