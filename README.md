# GuidVoting

### Description
This is a simple ASP.NET that can be used for voting with GUIDs.

- Uses MySQL/MariaDB for events/questions/choices/GUIDs.
- /admin page to Create Events, Questions, Choices, **AND** PDFs with those questions, choices, and a QR code that a user can scan to navigate to vote
- /vote page to do the actual voting

## Secrets
```
public class Secrets
{
	public static MySqlConnection GetConnectionString() => new($"Server={MySqlUrl};Database={MySqlDb};Uid={MySqlUsername};Pwd={MySqlPassword};SSL Mode=Required");

	private const string MySqlUrl = "domain.com";
	private const string MySqlUsername = "guidvoting";
	private const string MySqlPassword = "yourpassword";
	private const string MySqlDb = "guidvoting";
}
```

## CREATE TABLEs
```
CREATE TABLE IF NOT EXISTS `choices` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `QuestionId` int(11) NOT NULL,
  `ChoiceText` varchar(255) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `QuestionId` (`QuestionId`),
  CONSTRAINT `choices_ibfk_1` FOREIGN KEY (`QuestionId`) REFERENCES `questions` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `events` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(255) NOT NULL,
  `DateCreated` date DEFAULT curdate(),
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `questions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Text` varchar(500) NOT NULL,
  `EventId` int(11) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `EventId` (`EventId`),
  CONSTRAINT `questions_ibfk_1` FOREIGN KEY (`EventId`) REFERENCES `events` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `votes` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `QuestionId` int(11) NOT NULL,
  `ChoiceId` int(11) NOT NULL,
  `Guid` text NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `QuestionId` (`QuestionId`),
  KEY `ChoiceId` (`ChoiceId`),
  CONSTRAINT `votes_ibfk_1` FOREIGN KEY (`QuestionId`) REFERENCES `questions` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `votes_ibfk_2` FOREIGN KEY (`ChoiceId`) REFERENCES `choices` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `vote_guids` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `EventId` int(11) NOT NULL DEFAULT 0,
  `Guid` text NOT NULL,
  PRIMARY KEY (`id`),
  KEY `FK__events` (`EventId`),
  CONSTRAINT `FK__events` FOREIGN KEY (`EventId`) REFERENCES `events` (`Id`) ON DELETE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;
```