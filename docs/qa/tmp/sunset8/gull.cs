// Play: 갈매기 거리·높이 = (__D__, __Y__) 로 바꾸고 지금 바로 한 번 날림
var look = SunsetLook.Find(); var db = look.backdrop.GetComponent<DuskBackdrop>();
db.gulls = new Vector4(3f, __D__f, __Y__f, 6f);
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
typeof(DuskBackdrop).GetField("flightT0", bf).SetValue(db, -1f);
typeof(DuskBackdrop).GetMethod("StartFlight", bf).Invoke(db, new object[] { Time.time });
return "gulls=" + db.gulls + " flights=" + db.Flights;
