using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Contenu narratif du MVP pour les civilisations Phenicie et Grece.
    /// Fournit une methode statique pour remplir la liste d'un EventManager.
    /// </summary>
    public static class NarrativeEventsContent
    {
        /// <summary>
        /// Genere tous les evenements narratifs du MVP et les ajoute a l'EventManager.
        /// </summary>
        public static void PopulateEventManager(EventManager eventManager)
        {
            if (eventManager == null) return;

            // === INTERLUDES D'ERE ===

            // Phenicie : Le Crepuscule de Tyr (Antiquite -> Classique)
            CreateInterlude(eventManager, 100, "Le Crepuscule de Tyr",
                "Le roi Hiram Ier, batisseur du Grand Temple de Tyr, sent la mort approcher.\n\n" +
                "Son regard embrasse la mer depuis les remparts de la cite pourpre. Les navires " +
                "pheniciens dansent sur les vagues, portant l'ambre, l'ebene et l'ivoire aux quatre " +
                "coins de la Mediterranee.\n\n" +
                "\"Mon corps rejoint les cendres, mais l'esprit de Tyr vit dans le bois de ses coques,\n" +
                "dans la pourpre de ses voiles, dans le regard de ses capitaines.\"\n\n" +
                "Il se tourne vers Elissa, sa niece. Le conseil retient son souffle.",
                0, // Antiquity era
                new[] { 0 }, // Phoenicia (CivId 0)
                new[] { "TurnCount:20" },
                new ChoiceData[] {
                    new ChoiceData {
                        ChoiceText = "Elissa prend la mer vers le couchant",
                        ChoiceDescription = "Elle fonde une nouvelle cité sur la cote africaine, portant la gloire de Tyr au-dela des colonnes d'Hercules.",
                        Effects = new[] { "+100 gold", "+15 science", "Unlock: Colony", "Legacy: Foundation of Carthage" },
                        LegacyUnlock = "Foundation of Carthage",
                        NarrativeFollowUp = "Les voiles rouges disparaissent a l'horizon. Elissa emporte avec elle les rites sacres, " +
                            "les artisans du verre et les marchands les plus audacieux. La legende raconte qu'elle acheta " +
                            "autant de terre que pouvait en couvrir une peau de boeuf — puis decoupa la peau en fines " +
                            "lanieres pour ceindre toute une colline. Ainsi naquit Carthage."
                    },
                    new ChoiceData {
                        ChoiceText = "Le conseil des anciens garde le pouvoir a Tyr",
                        ChoiceDescription = "Les marchands et les pretres renforcent la ligue phenicienne pour dominer les routes maritimes depuis la metropole.",
                        Effects = new[] { "+200 gold", "+20 gold par tour", "+10 culture", "Legacy: League of Tyre" },
                        LegacyUnlock = "League of Tyre",
                        NarrativeFollowUp = "Les vieux murs de Tyr retentissent des debats du conseil. Les marchands scellent " +
                            "un pacte qui unifie les comptoirs pheniciens sous la banniere de la cite-mere. Les navires " +
                            "ne partent plus sans la benediction du Grand Temple. La puissance de Tyr rayonne, " +
                            "mais ses frontieres restent celles d'une ile — protectrices, etroitess."
                    }
                });

            // Phenicie : L'Héritage de Didon (Classique -> Medievale)
            CreateInterlude(eventManager, 101, "L'Héritage de Didon",
                "Carthage n'est plus un comptoir — c'est une puissance.\n\n" +
                "Les annees ont passe. La reine Elissa — que le peuple nomme Didon — contemple " +
                "la ville blanche accrochee a la colline de Byrsa. Les navires carthaginois " +
                "rivalisent desormais avec ceux de Tyr elle-meme.\n\n" +
                "Mais une ombre grandit a l'horizon. Les cites grecques de Sicile regardent " +
                "Carthage d'un oeil inquiet. Et quelque part, au-dela des detroits, un navigateur " +
                "nomme Hannon prepare ses vaisseaux pour l'inconnu.",
                1, // Classical era
                new[] { 0 },
                new[] { "Era:1" },
                new ChoiceData[] {
                    new ChoiceData {
                        ChoiceText = "Etendre le reseau marchand vers l'Atlantique",
                        ChoiceDescription = "Hannon navigue vers le sud, fondant des comptoirs sur la cote africaine. Le commerce rapporte des richesses incalculables.",
                        Effects = new[] { "+300 gold", "+15 science par tour", "+5 culture par tour", "Legacy: Hannon's Navigation" },
                        LegacyUnlock = "Hannon's Navigation",
                        NarrativeFollowUp = "Hannon leve l'ancre par un matin d'ete. Sa flotte de soixante penteres longe " +
                            "la cote africaine, depassant les colonnes d'Hercules pour trouver de nouveaux horizons. " +
                            "Chaque escale est un nouveau marche. Chaque cap franchi, une promesse de fortune. " +
                            "Les recits de son voyage deviendront la carte des generations futures."
                    },
                    new ChoiceData {
                        ChoiceText = "Fortifier Carthage contre les convoitises",
                        ChoiceDescription = "Didon renforce les murailles et l'armee carthaginoise. La securite avant l'expansion.",
                        Effects = new[] { "+150 gold", "+25 culture", "Unlock: Fortifications", "Legacy: Walls of Byrsa" },
                        LegacyUnlock = "Walls of Byrsa",
                        NarrativeFollowUp = "Les murailles de Byrsa s'elevent, massives, blanches sous le soleil africain. " +
                            "Les Grecs regardent depuis leurs trieres, mesurant la puissance nouvelle de Carthage. " +
                            "Didon sait que la paix ne dure qu'autant que la peur. Elle fait graver sur la porte " +
                            "principale : 'Ici commence un peuple qui ne sera jamais conquis.'"
                    }
                });

            // Grece : Le Serment Brisé (Antiquite -> Classique)
            CreateInterlude(eventManager, 200, "Le Serment Brisé",
                "La coalition d'Agamemnon se fissure.\n\n" +
                "Les cendres de Troie fument encore. Les heros sont morts ou repartis, " +
                "emportant leur gloire et leur rancune. Agamemnon, roi des rois, rentre a Mycene " +
                "le coeur lourd. Il a gagne la guerre, mais perdu l'unité des Hellènes.\n\n" +
                "Dans les palais, on chuchote. Les cites commencent a regarder ailleurs.\n" +
                "\"Un peuple uni n'a besoin que d'une seule voix,\" dit-il en fixant la mer Egée.\n" +
                "\"Mais quelle voix, quand les heros se taisent ?\"",
                0, // Antiquity era
                new[] { 1 }, // Greece (CivId 1)
                new[] { "TurnCount:20" },
                new ChoiceData[] {
                    new ChoiceData {
                        ChoiceText = "Pericles et la democratie athenienne",
                        ChoiceDescription = "Le pouvoir retourne au peuple d'Athenes. La culture et la philosophie fleurissent dans les agora.",
                        Effects = new[] { "+50 science", "+30 culture", "+5 culture par tour", "Legacy: Athenian Democracy" },
                        LegacyUnlock = "Athenian Democracy",
                        NarrativeFollowUp = "Sur la Pnyx, les citoyens d'Athenes se rassemblent. Pericles, jeune encore, " +
                            "prononce des paroles qui traverseront les siècles : 'Notre constitution est appelee " +
                            "democratie parce que le pouvoir est entre les mains non d'une minorite, mais du plus grand nombre.'" +
                            "La flotte athenienne domine desormais l'Egee."
                    },
                    new ChoiceData {
                        ChoiceText = "Maintenir l'alliance militaire spartiate",
                        ChoiceDescription = "Les rois de Sparte conservent la suprematie militaire. La Ligue du Peloponnese garantit la force par l'obéissance.",
                        Effects = new[] { "+100 gold", "+15 attaque pour toutes les unites", "Legacy: Spartan Hegemony" },
                        LegacyUnlock = "Spartan Hegemony",
                        NarrativeFollowUp = "Les hoplites spartiates martèlent le sol en cadence. Les ilotes travaillent " +
                            "les champs tandis que les guerriers s'entraînent sans relache. La Ligue se resserre autour " +
                            "de Sparte. Les messagers courent vers Delphes, Corinthe, Thebes. L'ombre de la guerre " +
                            "plane sur la Grece."
                    }
                });

            // Grece : Le Rêve d'Alexandre (Classique -> Medievale)
            CreateInterlude(eventManager, 201, "Le Rêve d'Alexandre",
                "La Grece n'est plus une constellation de cites rivales. Alexandre a tout conquis.\n\n" +
                "Depuis la fenêtre de son palais de Pella, le jeune roi contemple l'horizon. " +
                "Il a vingt ans. La Grece est a ses pieds. Mais la Perse l'attend, et au-dela, " +
                "des terres que les cartes n'osent pas nommer.\n\n" +
                "Aristote, son precepteur, lui a appris que le monde est une succession de mysteres. " +
                "Alexandre veut tous les percer. Mais ses generaux, eux, pensent aux moissons " +
                "et aux interets des marchands.",
                1, // Classical era
                new[] { 1 },
                new[] { "Era:1" },
                new ChoiceData[] {
                    new ChoiceData {
                        ChoiceText = "Conquerir vers l'Orient",
                        ChoiceDescription = "Alexandre leve une armee immense et marche vers la Perse. La gloire et les tresors de l'Orient l'attendent.",
                        Effects = new[] { "+500 gold", "+20 attaque pour toutes les unites", "+10 culture", "Legacy: Alexander's Empire" },
                        LegacyUnlock = "Alexander's Empire",
                        NarrativeFollowUp = "Les phalanges traversent l'Hellespont. A Granique, Issos, Gaugameles, les Perses " +
                            "tombent. Babylone ouvre ses portes. Alexandre pleure en voyant le tombeau d'Achille — " +
                            "il a depasse son heros. L'empire s'etend jusqu'aux confins du monde connu, portant " +
                            "la langue et la pensee grecques aux rives de l'Indus."
                    },
                    new ChoiceData {
                        ChoiceText = "Construire la grande academie d'Athenes",
                        ChoiceDescription = "Alexandre prefere les idees aux epées. Il finance la recherche, la philosophie et les arts.",
                        Effects = new[] { "+100 science", "+20 science par tour", "+50 culture", "+10 culture par tour", "Legacy: Library of Alexandria" },
                        LegacyUnlock = "Library of Alexandria",
                        NarrativeFollowUp = "Les plus grands esprits affluent vers Athenes. Euclide trace ses cercles, " +
                            "Aristote classifie le vivant, Aristophane fait rire les foules. La bibliotheque " +
                            "d'Alexandrie, merveille du monde, accueille les savoirs de toutes les nations. " +
                            "Le pouvoir d'Alexandre n'est pas dans ses armees mais dans les idees qu'il seme."
                    }
                });

            // === MOMENTS CLES ===

            // Phenicie : La Traversee
            CreateKeyMoment(eventManager, 110, "La Traversée",
                "La mer se dechaine autour de la flotte phenicienne.\n\n" +
                "Trois jours de tempete. Les navires sont disperses. Les vivres s'epuisent. " +
                "Les marins murmurent que les dieux sont en colere — qu'il est temps de rentrer.\n\n" +
                "Le capitaine, barbu et fatigue, se tourne vers vous. Les rivages de la " +
                "mer Interieure sont derriere. Devant, l'horizon infini de l'ocean.\n\n" +
                "\"Ma reine, la mer nous repousse. Mais peut-etre est-ce une epreuve, " +
                "pas un refus. Que decidez-vous ?\"",
                new[] { 0 },
                new[] { "TurnCount:40", "HasCityOnCoast" },
                new ChoiceData[] {
                    new ChoiceData {
                        ChoiceText = "Forcer le passage vers l'Atlantique",
                        ChoiceDescription = "Braver la tempête et les dieux. L'inconnu attend, avec ses promesses et ses dangers.",
                        Effects = new[] { "+200 gold", "+25 science", "+10 culture", "Legacy: Ocean Explorers" },
                        LegacyUnlock = "Ocean Explorers",
                        NarrativeFollowUp = "La flotte emerge de la tempete, epuisee mais victorieuse. Devant elles, " +
                            "des eaux jamais naviguees. Les Pheniciens sont les premiers a defier l'Atlantique. " +
                            "Les recits de ce voyage nourriront les reves des explorateurs pendant mille ans."
                    },
                    new ChoiceData {
                        ChoiceText = "Rebrousser chemin et fortifier les comptoirs",
                        ChoiceDescription = "La mer n'est pas une tombe. Mieux vaut revenir et consolider ce que l'on possede.",
                        Effects = new[] { "+200 gold", "+50 gold par tour", "+15 culture", "Legacy: Fortified Colonies" },
                        LegacyUnlock = "Fortified Colonies",
                        NarrativeFollowUp = "Les navires rentrent un a un, accueillis comme des revenants. " +
                            "Les lecons de la tempete sont gravees dans le marbre du conseil : les comptoirs " +
                            "pheniciens se parent de murailles, les cales se remplissent de reserves. " +
                            "La mer reprendra ses droits, mais Tyr sera prete."
                    }
                });

            // Grece : La Guerre des Cités
            CreateKeyMoment(eventManager, 210, "La Guerre des Cités",
                "La Grece saigne par ses propres mains.\n\n" +
                "Athenes et Sparte, les deux geants, se font face. Les allies ont pris parti. " +
                "Les temples sont fermes, les marches desertes. Les meres cachent leurs fils.\n\n" +
                "Une guerre civile grecque est une blessure qui ne guerit jamais. Chaque bataille " +
                "est une bataille entre freres. Mais ne pas choisir, c'est laisser la Grece se " +
                "dechirer jusqu'a ce qu'il ne reste plus que des cendres.\n\n" +
                "Le conseil attend votre decision. Les deux camps ont envoye des emissaires.",
                new[] { 1 },
                new[] { "TurnCount:40" },
                new ChoiceData[] {
                    new ChoiceData {
                        ChoiceText = "Soutenir Athenes et la democratie",
                        ChoiceDescription = "Athenes incarne la culture, la philosophie, la liberte. Son triomphe sera celui de l'esprit grec.",
                        Effects = new[] { "+50 science par tour", "+20 culture par tour", "+15 relations with Greece", "Legacy: Athenian Renaissance" },
                        LegacyUnlock = "Athenian Renaissance",
                        NarrativeFollowUp = "La flotte athenienne prend le large, portant le feu de la democratie sur toutes les iles. " +
                            "Les poetes celebrent la victoire de l'esprit sur la force. Mais dans les ruelles du Pirée, " +
                            "des veuves spartiates pleurent en silence. La Grece est unifiee, mais cicatrisee."
                    },
                    new ChoiceData {
                        ChoiceText = "Soutenir Sparte et l'ordre traditionnel",
                        ChoiceDescription = "Sparte est la force, la discipline, la continuite. Sa victoire assure la stabilite militaire.",
                        Effects = new[] { "+200 gold", "+15 attaque pour toutes les unites", "Legacy: Spartan Order" },
                        LegacyUnlock = "Spartan Order",
                        NarrativeFollowUp = "Les hoplites spartiates defilent dans Athenes vaincue, mais sans humilier les vaincus. " +
                            "Sparta impose une paix ferme mais juste. Les maisons brûlées seront reconstruites. " +
                            "Mais la flamme de la democratie couve, prete a renaître."
                    },
                    new ChoiceData {
                        ChoiceText = "Negocier une paix entre les camps",
                        ChoiceDescription = "Un compromis est possible. Les cites auront l'autonomie, mais la Grece parlera d'une seule voix a l'exterieur.",
                        Effects = new[] { "+100 gold", "+100 science", "+50 culture", "+30 relations with Greece", "Legacy: United Hellas" },
                        LegacyUnlock = "United Hellas",
                        NarrativeFollowUp = "Les emissaires se rencontrent a Corinthe, sur un terrain neutre. " +
                            "Les debats sont longs, parfois violents, mais au matin du septième jour, " +
                            "un traite est signe. La Ligue de Corinthe est nee — fragile, imparfaite, " +
                            "mais elle unit ce que la guerre voulait detruire. Pour la première fois, " +
                            "la Grece parle d'une seule voix."
                    }
                });

            // === MICRO-EVENEMENTS ===

            // Micro 1: Marchand etranger (generique)
            CreateMicroEvent(eventManager, 300, "Un marchand erranger propose des techniques de navigation",
                "Un marchand Phenicien aborde votre cour avec des rouleaux de papyrus couverts " +
                "de diagrammes. Il pretend connaître une route secret vers l'Orient.\n\n" +
                "\"Maitre, ces cartes valent leur pesant d'or. Ou... je peux vous apprendre " +
                "a les lire vous-même, pour le prix d'un bon repas et d'une place dans votre \"\n" +
                "port.\"",
                new[] { -1 }, // Toute civ
                new[] { "HasCityOnCoast" },
                new ChoiceData[] {
                    new ChoiceData { ChoiceText = "Acheter les cartes", ChoiceDescription = "Les routes secretes sont desormais tiennes.", Effects = new[] { "+100 science", "-30 gold" }, NarrativeFollowUp = "Les eaux s'ouvrent devant vous comme les pages d'un livre." },
                    new ChoiceData { ChoiceText = "L'embaucher comme instructeur", ChoiceDescription = "Vos navigateurs apprennent ses techniques.", Effects = new[] { "+50 science", "+5 science par tour", "-50 gold" }, NarrativeFollowUp = "Les jeunes capitaines prennent des notes, les yeux brillants." },
                    new ChoiceData { ChoiceText = "Le chasser", ChoiceDescription = "Pas de place pour les charlatans.", Effects = new[] { "-5 culture" }, NarrativeFollowUp = "Le marchand jure en s'eloignant, emportant ses secrets." }
                });

            // Micro 2: Prophete (generique)
            CreateMicroEvent(eventManager, 301, "Un prophete erre dans la campagne",
                "Un homme vetu de haillons se tient a la porte de votre cite. Il parle de " +
                "châtiments divins et de renouveau. Les foules commencent a l'ecouter.",
                new[] { -1 },
                System.Array.Empty<string>(),
                new ChoiceData[] {
                    new ChoiceData { ChoiceText = "L'accueillir et l'ecouter", ChoiceDescription = "Le prophete benit votre peuple, mais ses idees radicales sediment le doute.", Effects = new[] { "+20 culture", "+5 culture par tour", "-5 gold per turn" }, NarrativeFollowUp = "Le prophete parle, et les foules l'ecoutent." },
                    new ChoiceData { ChoiceText = "L'ignorer poliment", ChoiceDescription = "Il passe son chemin, mais les graines sont semeés.", Effects = new[] { "+10 culture" }, NarrativeFollowUp = "Le prophete s'en va, mais certains citoyens le suivent discretement." },
                    new ChoiceData { ChoiceText = "L'emprisonner", ChoiceDescription = "L'ordre avant la superstition.", Effects = new[] { "+5 gold", "-10 culture" }, NarrativeFollowUp = "Le prophete pourrit en prison, mais sa legende grandit." }
                });

            // Micro 3: Festival (generique)
            CreateMicroEvent(eventManager, 302, "Un festival spontane eclate sur la place publique",
                "Les musiciens accordent leurs lyres, les danseuses enroulent leurs voiles. " +
                "Le peuple est en fête et vous invite a proclamer un jour de celebration.",
                new[] { -1 },
                System.Array.Empty<string>(),
                new ChoiceData[] {
                    new ChoiceData { ChoiceText = "Organiser une grande fête", ChoiceDescription = "Le peuple vous aime. La culture rayonne.", Effects = new[] { "+30 culture", "-20 gold" }, NarrativeFollowUp = "Les celebrités durent trois jours. Le peuple vous acclame." },
                    new ChoiceData { ChoiceText = "Encourager les competitions sportives", ChoiceDescription = "Les athletes s'affrontent, forgeant de nouveaux heros.", Effects = new[] { "+15 culture", "+10 gold" }, NarrativeFollowUp = "Les jeux rassemblent les cites voisines dans une treve sacrée." },
                    new ChoiceData { ChoiceText = "Interdire les rassemblements", ChoiceDescription = "Le travail avant les plaisirs.", Effects = new[] { "+20 gold", "-15 culture" }, NarrativeFollowUp = "Les musiciens se taisent, mais les murmures persistent." }
                });

            // Micro 4: Tremblement de terre (generique negatif)
            CreateMicroEvent(eventManager, 303, "La terre tremble sous vos pieds",
                "Un grondement sourd monte des profondeurs. Les colonnes des temples vacillent. " +
                "Une partie du rempart sud s'effondre dans un nuage de poussière.",
                new[] { -1 },
                new[] { "TurnCount:30" },
                new ChoiceData[] {
                    new ChoiceData { ChoiceText = "Mobiliser les citoyens aux secours", ChoiceDescription = "Les dégâts sont limités. La solidarité renforce le peuple.", Effects = new[] { "+10 culture", "-10 gold" }, NarrativeFollowUp = "Les citoyens déblayent les ruines ensemble, plus unis que jamais." },
                    new ChoiceData { ChoiceText = "Prier les dieux et restaurer les temples", ChoiceDescription = "Les pretres apaisent la colère divine.", Effects = new[] { "+20 culture", "-50 gold" }, NarrativeFollowUp = "Les offrandes montent vers le ciel. Les tremblements cessent." },
                    new ChoiceData { ChoiceText = "Reconstruire sans attendre", ChoiceDescription = "Le genie civil s'active. Les nouveaux remparts sont plus solides.", Effects = new[] { "+10 science", "+10 gold par tour", "-80 gold" }, NarrativeFollowUp = "Un an plus tard, les remparts sont plus hauts et plus beaux qu'avant." }
                });

            // Micro 5: Phenicie - Decouverte d'une nouvelle cote
            CreateMicroEvent(eventManager, 310, "Des marins pheniciens decouvrent une nouvelle cote",
                "Les eclaireurs reviennent avec des histoires extravagantes : une terre au-dela " +
                "de la mer, peuplee d'habitants qui n'ont jamais vu de navire.\n\n" +
                "\"Maitre, des coquillages aux couleurs de pourpre tapissent les plages !\"",
                new[] { 0 },
                new[] { "HasCityOnCoast" },
                new ChoiceData[] {
                    new ChoiceData { ChoiceText = "Etablir un comptoir commercial", ChoiceDescription = "Le pourpre et les epices de cette terre rapporteront une fortune.", Effects = new[] { "+200 gold", "+10 gold par tour", "+10 science" }, NarrativeFollowUp = "Le comptoir prospère. Les autochtones échangent joyeusement." },
                    new ChoiceData { ChoiceText = "Cartographier les cotes", ChoiceDescription = "La connaissance est une richesse qui ne s'epuise jamais.", Effects = new[] { "+50 science", "+5 science par tour", "+10 culture" }, NarrativeFollowUp = "Les cartes s'enrichissent de nouveaux noms et promesses." }
                });

            // Micro 6: Grece - Philosophes dans l'agora
            CreateMicroEvent(eventManager, 311, "Des philosophes grecs debattent dans l'agora",
                "Socrate, Platon et Aristote — enfin, des hommes qui leur ressemblent — " +
                "discutent sous les colonnades de marbre. Les passants s'arretent. " +
                "Des questions fondamentales sont posees.",
                new[] { 1 },
                System.Array.Empty<string>(),
                new ChoiceData[] {
                    new ChoiceData { ChoiceText = "Financer la recherche philosophique", ChoiceDescription = "Les penseurs explorent les mystères de l'existence.", Effects = new[] { "+50 science", "+15 science par tour", "-30 gold" }, NarrativeFollowUp = "Les dialogues de Platon circulent dans tout le monde civilisé." },
                    new ChoiceData { ChoiceText = "Construire une ecole de rhetorique", ChoiceDescription = "Les orateurs formeront les citoyens de demain.", Effects = new[] { "+30 culture", "+10 culture par tour", "+5 science par tour", "-20 gold" }, NarrativeFollowUp = "L'école forme des debateurs redoutables et des esprits brillants." }
                });
        }

        // ----------------------------------------------------------------
        // Constructeurs d'evenements
        // ----------------------------------------------------------------

        private static void CreateInterlude(EventManager manager, int id, string title, string description,
            int triggerEra, int[] civIds, string[] conditions, ChoiceData[] choices)
        {
            CreateEvent(manager, id, title, description, EventType.Interlude,
                triggerEra, civIds, conditions, choices);
        }

        private static void CreateKeyMoment(EventManager manager, int id, string title, string description,
            int[] civIds, string[] conditions, ChoiceData[] choices)
        {
            CreateEvent(manager, id, title, description, EventType.KeyMoment,
                -1, civIds, conditions, choices);
        }

        private static void CreateMicroEvent(EventManager manager, int id, string title, string description,
            int[] civIds, string[] conditions, ChoiceData[] choices)
        {
            CreateEvent(manager, id, title, description, EventType.Micro,
                -1, civIds, conditions, choices);
        }

        private static void CreateEvent(EventManager manager, int id, string title, string description,
            EventType type, int triggerEra, int[] civIds, string[] conditions, ChoiceData[] choices)
        {
            var evt = ScriptableObject.CreateInstance<EventData>();
            evt.EventId = id;
            evt.Title = title;
            evt.Description = description;
            evt.Type = type;
            evt.TriggerEra = triggerEra;
            evt.TriggerCivIds = civIds;
            evt.TriggerConditions = conditions;
            evt.Choices = choices;

            // Ajouter via la methode publique
            manager.RegisterEvent(evt);

            Debug.Log($"[NarrativeContent] Evenement cree : {title} (ID {id}, type {type})");
        }
    }
}
