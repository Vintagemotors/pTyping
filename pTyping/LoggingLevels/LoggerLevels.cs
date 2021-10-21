using Kettu;

namespace pTyping.LoggingLevels {
    public class LoggerLevelSongInfo : LoggerLevel {
        public override string Name => "SongInfo";
    }

    public class LoggerLevelSongManagerUpdateInfo : LoggerLevel {
        public override string Name => "SongManagerInfo";
    }

    public class LoggerLevelOnlineInfo : LoggerLevel {
        public override string Name => "OnlineInfo";
    }
    
    public class LoggerLevelChatMessage : LoggerLevel {
        public override string Name => "ChatMessage";
    }
}
