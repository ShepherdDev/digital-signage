(function ($)
{
  $.fn.digitalSign = function (options)
  {
    var $dsrContainer = this.first();
    var $fluxContainer = null;
    var flux = null;
    var audioTracks = [];
    var lastHash = '';
    var $audio = $('<audio />').appendTo($dsrContainer);
    var newSlideShowData = null;
    var settings = $.extend({
      animationDuration: 400,
      slideInterval: 8000,
      updateInterval: 60000,
      transitions: ['bars', 'blinds', 'blocks', 'blocks2', 'dissolve', 'slide', 'zip', 'bars3d', 'blinds3d', 'cube', 'tiles3d', 'turn'],
      device: 0,
      contentChannel: null
    }, options);

    //
    // Begin playing where we left off.
    //
    function play(nextSlide)
    {
      if (flux && !flux.isPlaying())
      {
        if ($fluxContainer.css('opacity') != 1 || !$fluxContainer.is(':visible'))
        {
          $fluxContainer.fadeIn(settings.animationDuration, function ()
          {
            if (nextSlide)
            {
              flux.next();
            }
          });
        }
        else if (nextSlide)
        {
          flux.next();
        }

        flux.start();
      }

      if ($audio.attr('src'))
      {
        $audio.get(0).play();
        $audio.animate({ volume: 1 }, settings.animationDuration);
      }
    }

    //
    // Begin playing the current slideshow or setup the new data.
    //
    function playOrSetup(nextSlide)
    {
      //
      // If we have new slide show data then lets load it up.
      //
      if (newSlideShowData != null)
      {
        pausePlayback(function ()
        {
          if ($fluxContainer != null)
          {
            $fluxContainer.remove();
            $fluxContainer = null;
          }

          flux = null;
          if ($audio.attr('src'))
          {
            $audio.attr('src', '');
          }

          setup(newSlideShowData);
          newSlideShowData = null;

          play(nextSlide);
        });
      }
      else
      {
        play(nextSlide);
      }
    }

    //
    // Pause audio and flux playback, fade everything out nicely and then
    // call the callback function.
    //
    function pausePlayback(callback)
    {
      $audio.animate({ volume: 0 }, settings.animationDuration);

      if (flux != null)
      {
        //
        // Stop flex and cleanup.
        //
        flux.stop();

        //
        // Fade out the current display and setup the screen again.
        //
        $fluxContainer.fadeOut(settings.animationDuration, function ()
        {
          $audio.get(0).pause();

          callback();
        });
      }
      else
      {
        setTimeout(function ()
        {
          $audio.get(0).pause();

          callback();
        }, settings.animationDuration);
      }
    }

    //
    // Setup all the various players with the data we have gotten
    // from the server.
    //
    function setup(data)
    {
      //
      // Prepare the container and determine the width and height to work with.
      //
      $fluxContainer = $('<div class="fluxContainer"></div>').appendTo($dsrContainer);
      var width = $fluxContainer.width();
      var height = $fluxContainer.height();
      var resizeArgs = '&width=' + width + '&height=' + height + '&bgcolor=black&scale=both';
      var hasVideo = false;

      //
      // Setup all the image nodes.
      //
      for (var i = 0; i < data.Contents.Slides.length; i++)
      {
        var video = parseVideo(data.Contents.Slides[i]);

        if (video.type != null)
        {
          $('<img src="/Plugins/com_shepherdchurch/DigitalSignage/Assets/4k-black.png" data-video="' + data.Contents.Slides[i] + '" />').appendTo($fluxContainer);
          hasVideo = true;
        }
        else
        {
          $('<img src="' + data.Contents.Slides[i] + resizeArgs + '" />').appendTo($fluxContainer);
        }
      }
      
      //
      // If we have a single slide and it is a video slide then add another black slide
      // as a placeholder for transitions.
      //
      if ($fluxContainer.find('img').length == 1 && hasVideo)
      {
        $('<img src="/Plugins/com_shepherdchurch/DigitalSignage/Assets/4k-black.png" />').appendTo($fluxContainer);
      }

      //
      // If there is more than 1 slide element then begin the flux transitions.
      //
      if ($fluxContainer.find('img').length >= 2)
      {
        //
        // Initialize the flux slider.
        //
        flux = new window.flux.slider('.fluxContainer', {
          pagination: false,
          autoplay: false,
          width: $dsrContainer.width(),
          height: $dsrContainer.height(),
          transitions: settings.transitions,
          delay: settings.slideInterval,
          onTransitionEnd: transitionEnd
        });
      }

      //
      // Prepare the audio tracks.
      //
      audioTracks = [];
      for (var i = 0; i < data.Contents.Audio.length; i++)
      {
        audioTracks.push(data.Contents.Audio[i]);
      }

      //
      // Initialize the audio playback.
      //
      if (audioTracks.length > 0)
      {
        $audio.attr('src', audioTracks[0]);
        $audio.prop('volume', 1);
      }

      lastHash = data.Hash;
    }

    //
    // A transition to a new slide has finished. If this is a video slide then switch
    // to playing the video. Also check if we have new data to load in.
    //
    function transitionEnd(data)
    {
      if (newSlideShowData != null)
      {
        playOrSetup();
      }
      else if ($(data.currentImage).data('video'))
      {
        pausePlayback(function () { playVideo($(data.currentImage).data('video')); });
      }
    }

    //
    // Parse a video URL into the provider and the video ID.
    // https://stackoverflow.com/questions/5612602/improving-regex-for-parsing-youtube-vimeo-urls
    //
    function parseVideo(url)
    {
      // - Supported YouTube URL formats:
      //   - http://www.youtube.com/watch?v=My2FRPA3Gf8
      //   - http://youtu.be/My2FRPA3Gf8
      //   - https://youtube.googleapis.com/v/My2FRPA3Gf8
      // - Supported Vimeo URL formats:
      //   - http://vimeo.com/25451551
      //   - http://player.vimeo.com/video/25451551
      // - Also supports relative URLs:
      //   - //player.vimeo.com/video/25451551

      url.match(/(http:\/\/|https:\/\/|)(player.|www.)?(vimeo\.com|youtu(be\.com|\.be|be\.googleapis\.com))\/(video\/|embed\/|watch\?v=|v\/)?([A-Za-z0-9._%-]*)(\&\S+)?/);
      var type = null;
      if (RegExp.$3.indexOf('youtu') > -1)
      {
        type = 'youtube';
      } else if (RegExp.$3.indexOf('vimeo') > -1)
      {
        type = 'vimeo';
      }

      return {
        type: type,
        id: RegExp.$6
      };
    }

    //
    // Play a video. This should only be called after the flux player has
    // been stopped.
    //
    function playVideo(videoUrl)
    {
      var video = parseVideo(videoUrl);

      if (video.type == 'vimeo')
      {
        playVimeoVideo(video.id);
      }
      else if (video.type == 'youtube')
      {
        playYoutubeVideo(video.id);
      }
      else
      {
        playOrSetup();
      }
    }

    //
    // Play a Vimeo video with the given Video Id.
    //
    function playVimeoVideo(videoId)
    {
      var $vid = null;
      var videoUrl = '//player.vimeo.com/video/' + videoId + '?autoplay=1';
      var timer = setTimeout(function () { restartPlayback(); }, 10000);

      // Restart playback if the iframe timed out or video finished.
      function restartPlayback()
      {
        $vid.remove();
        playOrSetup(true);
      }

      //
      // Load the video in an iframe. If it doesn't load within 5 seconds assume there was
      // an error and restart playback.
      //
      $vid = $('<iframe src="' + videoUrl + '" width="100%" height="100%" frameborder="0"><iframe>').hide();
      $vid.on('load', function ()
      {
        clearTimeout(timer);

        var player = new Vimeo.Player($vid.get(0));
        player.on('ended', function ()
        {
          restartPlayback();
        });

        $vid.fadeIn(settings.animationDuration);
      });

      $vid.insertBefore($fluxContainer);
    }

    //
    // Play a Youtube video with the given Video Id.
    //
    function playYoutubeVideo(videoId)
    {
      var $vid = null;
      var videoUrl = '//www.youtube.com/embed/' + videoId + '?enablejsapi=1&autoplay=1&rel=0&controls=0&fs=0&modestbranding=1&showinfo=0';
      var timer = setTimeout(function () { restartPlayback(); }, 10000);

      // Restart playback if the iframe timed out or video finished.
      function restartPlayback()
      {
        $vid.remove();
        playOrSetup(true);
      }

      function onPlayerStateChange(event)
      {
        if (event.data == YT.PlayerState.ENDED)
        {
          restartPlayback();
        }
      }

      //
      // Load the video in an iframe. If it doesn't load within 5 seconds assume there was
      // an error and restart playback.
      //
      $vid = $('<iframe id="dsrYoutube" src="' + videoUrl + '" width="100%" height="100%" frameborder="0"><iframe>').hide();
      $vid.on('load', function ()
      {
        clearTimeout(timer);

        var player = new YT.Player('dsrYoutube', {
          events: {
            'onStateChange': onPlayerStateChange
          }
        });

        $vid.fadeIn(settings.animationDuration);
      });

      $vid.insertBefore($fluxContainer);
    }

    //
    // Check the server to see if the feed has been updated.
    //
    function updateFeed()
    {
      var url = '/api/com.shepherdchurch/DigitalSignage/Device/' + settings.device;

      if (settings.contentChannel)
      {
        url = '/api/com.shepherdchurch/DigitalSignage/ContentChannel/' + settings.contentChannel;
      }

      $.ajax(url)
          .done(function (data)
          {
            if (data.Hash == lastHash)
            {
              return;
            }

            newSlideShowData = data;
            if (flux == null)
            {
              playOrSetup();
            }
          });
    }

    //
    // When an audio track ends, start the next.
    //
    $audio.on('ended', function ()
    {
      var index = audioTracks.indexOf($audio.attr('src')) + 1;

      if (index >= audioTracks.length)
      {
        index = 0;
      }

      if (audioTracks.length > 0)
      {
        $audio.attr('src', audioTracks[index]);
        $audio.get(0).play();
      }
    });

    //
    // When audio starts playing, fade it in.
    //
    $audio.on('play', function ()
    {
      $audio.animate({ volume: 1 }, settings.animationDuration);
    });

    updateFeed();
    setInterval(updateFeed, settings.updateInterval);

    return this;
  };
})(jQuery);
