namespace TreeClimberCore.Samples
{
    public static class SampleJSON
    {
        /*These get put in html as code samples, cannot be tabbed at all*/

        public const string RECIPE_SAMPLE = @"
{
    ""id"": ""0001"",
    ""type"": ""donut"",
    ""name"": ""Cake"",
    ""ppu"": 0.55,
    ""batters"":
	    {
		    ""batter"":
			    [
				    { ""id"": ""1001"", ""type"": ""Regular"" },
				    { ""id"": ""1002"", ""type"": ""Chocolate"" },
				    { ""id"": ""1003"", ""type"": ""Blueberry"" },
				    { ""id"": ""1004"", ""type"": ""Devil's Food"" }
			    ]
	    },
    ""topping"":
	    [
		    { ""id"": ""5001"", ""type"": ""None"" },
		    { ""id"": ""5002"", ""type"": ""Glazed"" },
		    { ""id"": ""5005"", ""type"": ""Sugar"" },
		    { ""id"": ""5007"", ""type"": ""Powdered Sugar"" },
		    { ""id"": ""5006"", ""type"": ""Chocolate with Sprinkles"" },
		    { ""id"": ""5003"", ""type"": ""Chocolate"" },
		    { ""id"": ""5004"", ""type"": ""Maple"" }
	    ]
}";

		public const string QUIZ_SAMPLE = @"
{
    ""quiz"": {
        ""sport"": {
            ""q1"": {
                ""question"": ""Which one is correct team name in NBA?"",
                ""options"": [
                    ""New York Bulls"",
                    ""Los Angeles Kings"",
                    ""Golden State Warriros"",
                    ""Huston Rocket""
                ],
                ""answer"": ""Huston Rocket""
            }
        },
        ""maths"": {
            ""q1"": {
                ""question"": ""5 + 7 = ?"",
                ""options"": [
                    ""10"",
                    ""11"",
                    ""12"",
                    ""13""
                ],
                ""answer"": ""12""
            },
            ""q2"": {
                ""question"": ""12 - 8 = ?"",
                ""options"": [
                    ""1"",
                    ""2"",
                    ""3"",
                    ""4""
                ],
                ""answer"": ""4""
            }
        }
    }
}";

        public const string GLOSSORY_SAMPLE = @"
{
    ""glossary"": {
	""GlossList"": {
        ""Entry1"": {
            ""ID"": ""SGML"",
			""SortAs"": ""SGML"",
			""GlossTerm"": ""Standard Generalized Markup Language"",
			""Acronym"": ""SGML"",
			""Abbrev"": ""ISO 8879:1986"",
			""GlossDef"": {
                ""para"": ""A meta-markup language."",
				""GlossSeeAlso"": [""GML"", ""XML""]
            },
			""GlossSee"": ""markup""
            },
        ""Entry2"": {
            ""ID"": ""SGML"",
			""SortAs"": ""SGML"",
			""GlossTerm"": ""Standard Generalized Markup Language"",
			""Acronym"": ""SGML"",
			""Abbrev"": ""ISO 8879:1986"",
			""GlossDef"": {
                ""para"": ""A meta-markup language."",
				""GlossSeeAlso"": [""GML"", ""XML""]
            },
			""GlossSee"": ""markup""
            },
        }   
    }
}";

        public const string SUPER_HERO_SAMPLE = @"
{
    ""squadName"": ""Super hero squad"",
    ""homeTown"": ""Metro City"",
    ""formed"": 2016,
    ""secretBase"": ""Super tower"",
    ""active"": true,
    ""members"": [
    {
        ""name"": ""Molecule Man"",
        ""age"": 29,
        ""secretIdentity"": ""Dan Jukes"",
        ""powers"": [
            ""Radiation resistance"", 
            ""Turning tiny"", 
            ""Radiation blast""
        ]
    },
    {
        ""name"": ""Madame Uppercut"",
        ""age"": 39,
        ""secretIdentity"": ""Jane Wilson"",
        ""powers"": [
            ""Million tonne punch"",
            ""Damage resistance"",
            ""Superhuman reflexes""
        ]
    },
    {
        ""name"": ""Eternal Flame"",
        ""age"": 1000,
        ""secretIdentity"": ""Unknown"",
        ""powers"": [
            ""Immortality"",
            ""Heat Immunity"",
            ""Inferno"",
            ""Teleportation"",
            ""Interdimensional travel""
        ]
    }
    ]
}";
    }
}
