return typeof(SeaWaves).Assembly.GetName().Name + " probe=" + typeof(FishingRippleProbe).Name + " clipField=" + (typeof(FishingSpot).GetField("splashClip") != null);
