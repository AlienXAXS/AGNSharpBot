using CommandHandler;
using Discord;
using Discord.WebSocket;
using Figgle;
using System.Collections.Generic;

namespace Responses.Commands.Global
{
    internal class ASCIIArt
    {
        [Command("asciiart",
            "Renders inputted text as ascii art, with over 250 fonts - use !asciiart help for more information")]
        [Alias("art")]
        [Permissions(Permissions.PermissionTypes.Guest)]
        public async void AsciiArt(string[] parameters, SocketMessage sktMessage,
            DiscordSocketClient discordSocketClient)
        {
            if (parameters.Length < 2)
            {
                await sktMessage.Channel.SendMessageAsync($"Too few parameters");
                return;
            }

            switch (parameters[1].ToLower())
            {
                case "help":
                    await sktMessage.Channel.SendMessageAsync("\r\n`ASCII Art Help`\r\n" +
                                                              "`!asciiart help` - This help\r\n" +
                                                              "`!asciiart fonts` - PM's you a list of fonts that can be used\r\n" +
                                                              "`!asciiart render FontName TEXT` - Renders the supplied text in ASCII Art, uses Standard font if you did not supply one (example: !asciiart render 1row \"Hello World!\")");
                    break;

                case "fonts":
                    ShowFonts(sktMessage);
                    break;

                case "render":
                    Render(parameters, sktMessage);
                    break;
            }
        }

        private async void ShowFonts(SocketMessage sktMessage)
        {
            var fontList = new List<string>
            {
                "1row", "3 - d", "3d_diagonal", "3x5", "4max", "5lineoblique", "acrobatic", "alligator", "alligator2", "alligator3", "alpha", "alphabet",
                "amc3line", "amc3liv1", "amcaaa01", "amcneko", "amcrazo2", "amcrazor", "amcslash", "amcslder", "amcthin", "amctubes", "amcun1", "arrows",
                "ascii_new_roman",  "avatar", "B1FF", "banner", "banner3", "banner3 - D", "banner4", "barbwire", "basic", "bear", "bell", "benjamin",
                "big", "bigchief", "bigfig", "binary", "block", "blocks", "bolger", "braced", "bright", "broadway", "broadway_kb", "bubble", "bulbhead",
                "calgphy2", "caligraphy", "cards", "catwalk", "chiseled", "chunky", "coinstak",  "cola", "colossal", "computer", "contessa", "contrast",
                "cosmic", "cosmike", "crawford", "crazy", "cricket", "cursive", "cyberlarge", "cybermedium", "cybersmall", "cygnet", "DANC4", "dancingfont",
                "decimal", "defleppard", "diamond", "dietcola", "digital", "doh", "doom", "dosrebel", "dotmatrix", "double", "doubleshorts", "drpepper",
                "dwhistled", "eftichess", "eftifont", "eftipiti", "eftirobot", "eftitalic", "eftiwall", "eftiwater", "epic", "fender", "filter", "fire_font - k",
                "fire_font - s", "flipped", "flowerpower", "fourtops", "fraktur", "funface", "funfaces", "fuzzy", "georgi16", "Georgia11", "ghost", "ghoulish",
                "glenyn",  "goofy", "gothic", "graceful", "gradient", "graffiti", "greek",  "heart_left", "heart_right", "henry3d", "hex", "hieroglyphs",
                "hollywood", "horizontalleft", "horizontalright", "ICL - 1900", "impossible", "invita", "isometric1", "isometric2", "isometric3", "isometric4",
                "italic", "ivrit", "jacky", "jazmine", "jerusalem", "katakana", "kban", "keyboard", "knob", "konto", "kontoslant", "larry3d", "lcd", "lean",
                "letters", "lildevil", "lineblocks", "linux", "lockergnome", "madrid", "marquee", "maxfour", "merlin1", "merlin2", "mike", "mini", "mirror",
                "mnemonic", "modular", "morse", "morse2", "moscow", "mshebrew210", "muzzle", "nancyj", "nancyj - fancy", "nancyj - improved", "nancyj - underlined",
                "nipples", "nscript", "ntgreek", "nvscript", "o8", "octal", "ogre", "oldbanner", "os2", "pawp", "peaks", "peaksslant", "pebbles", "pepper",
                "poison", "puffy", "puzzle", "pyramid", "rammstein", "rectangles", "red_phoenix", "relief", "relief2", "rev", "reverse", "roman", "rot13",
                "rotated", "rounded", "rowancap", "rozzo", "runic", "runyc", "santaclara", "sblood", "script", "slscript", "serifcap", "shadow", "shimrod",
                "short", "slant", "slide", "small", "smallcaps", "smisome1", "smkeyboard", "smpoison", "smscript", "smshadow", "smslant", "smtengwar", "soft",
                "speed", "spliff", "s - relief", "stacey", "stampate", "stampatello", "standard", "starstrips", "starwars", "stellar", "stforek", "stop",
                "straight", "sub - zero", "swampland", "swan", "sweet",  "tanja", "tengwar", "term", "test1", "thick", "thin", "threepoint", "ticks",
                "ticksslant", "tiles", "tinker - toy", "tombstone", "train", "trek", "tsalagi", "tubular", "twisted", "twopoint", "univers","usaflag",
                "varsity",  "wavy", "weird", "wetletter", "whimsy", "wow"
            };

            string compiledFontList = "";
            await sktMessage.Author.SendMessageAsync("`Font List will be posted below.`");

            var i = 0;
            while (i != fontList.Count)
            {
                compiledFontList += $"{i} - {fontList[i]}\r\n";

                if (compiledFontList.Length > 1500)
                {
                    await sktMessage.Author.SendMessageAsync(compiledFontList);
                    compiledFontList = "";
                }

                i++;
            }

            await sktMessage.Author.SendMessageAsync(compiledFontList);
            await sktMessage.Author.SendMessageAsync("`Font List post complete.`");
        }

        private string DiscordRenderedText(string text)
        {
            return $"```\r\n{text}\r\n```";
        }

        private async void Render(string[] parameters, SocketMessage sktMessage)
        {
            /*
             * !asciiart render "font" "text message"
             * !asciiart render "text message"
             */
            if (parameters.Length < 3)
            {
                await sktMessage.Channel.SendMessageAsync(
                    "Too few parameters for Render, use !asciiart help for help!");
                return;
            }

            string pickedFont = "standard";
            string message = null;

            // If we're 4 - then we have a font defined
            if (parameters.Length == 4)
            {
                pickedFont = parameters[2];
                message = parameters[3];
            }
            else
                message = parameters[2];

            string renderedMessage = null;
            switch (pickedFont)
            {
                case "1row":
                    renderedMessage = FiggleFonts.OneRow.Render(message);
                    break;

                case "3 - d":
                    renderedMessage = FiggleFonts.ThreeD.Render(message);
                    break;

                case "3d_diagonal":
                    renderedMessage = FiggleFonts.ThreeDDiagonal.Render(message);
                    break;

                case "3x5":
                    renderedMessage = FiggleFonts.ThreeByFive.Render(message);
                    break;

                case "4max":
                    renderedMessage = FiggleFonts.FourMax.Render(message);
                    break;

                case "5lineoblique":
                    renderedMessage = FiggleFonts.FiveLineOblique.Render(message);
                    break;

                case "acrobatic":
                    renderedMessage = FiggleFonts.Acrobatic.Render(message);
                    break;

                case "alligator":
                    renderedMessage = FiggleFonts.Alligator.Render(message);
                    break;

                case "alligator2":
                    renderedMessage = FiggleFonts.Alligator2.Render(message);
                    break;

                case "alligator3":
                    renderedMessage = FiggleFonts.Alligator3.Render(message);
                    break;

                case "alpha":
                    renderedMessage = FiggleFonts.Alpha.Render(message);
                    break;

                case "alphabet":
                    renderedMessage = FiggleFonts.Alphabet.Render(message);
                    break;

                case "amc3line":
                    renderedMessage = FiggleFonts.Amc3Line.Render(message);
                    break;

                case "amc3liv1":
                    renderedMessage = FiggleFonts.Amc3Liv1.Render(message);
                    break;

                case "amcaaa01":
                    renderedMessage = FiggleFonts.AmcAaa01.Render(message);
                    break;

                case "amcneko":
                    renderedMessage = FiggleFonts.AmcNeko.Render(message);
                    break;

                case "amcrazo2":
                    renderedMessage = FiggleFonts.AmcRazor2.Render(message);
                    break;

                case "amcrazor":
                    renderedMessage = FiggleFonts.AmcRazor.Render(message);
                    break;

                case "amcslash":
                    renderedMessage = FiggleFonts.AmcSlash.Render(message);
                    break;

                case "amcslder":
                    renderedMessage = FiggleFonts.AmcSlder.Render(message);
                    break;

                case "amcthin":
                    renderedMessage = FiggleFonts.AmcThin.Render(message);
                    break;

                case "amctubes":
                    renderedMessage = FiggleFonts.AmcTubes.Render(message);
                    break;

                case "amcun1":
                    renderedMessage = FiggleFonts.AmcUn1.Render(message);
                    break;

                case "arrows":
                    renderedMessage = FiggleFonts.Arrows.Render(message);
                    break;

                case "ascii_new_roman":
                    renderedMessage = FiggleFonts.AsciiNewroman.Render(message);
                    break;

                case "avatar":
                    renderedMessage = FiggleFonts.Avatar.Render(message);
                    break;

                case "B1FF":
                    renderedMessage = FiggleFonts.B1FF.Render(message);
                    break;

                case "banner":
                    renderedMessage = FiggleFonts.Banner.Render(message);
                    break;

                case "banner3":
                    renderedMessage = FiggleFonts.Banner3.Render(message);
                    break;

                case "banner3 - D":
                    renderedMessage = FiggleFonts.Banner3D.Render(message);
                    break;

                case "banner4":
                    renderedMessage = FiggleFonts.Banner4.Render(message);
                    break;

                case "barbwire":
                    renderedMessage = FiggleFonts.BarbWire.Render(message);
                    break;

                case "basic":
                    renderedMessage = FiggleFonts.Basic.Render(message);
                    break;

                case "bear":
                    renderedMessage = FiggleFonts.Bear.Render(message);
                    break;

                case "bell":
                    renderedMessage = FiggleFonts.Bell.Render(message);
                    break;

                case "benjamin":
                    renderedMessage = FiggleFonts.Benjamin.Render(message);
                    break;

                case "big":
                    renderedMessage = FiggleFonts.Big.Render(message);
                    break;

                case "bigchief":
                    renderedMessage = FiggleFonts.BigChief.Render(message);
                    break;

                case "bigfig":
                    renderedMessage = FiggleFonts.BigFig.Render(message);
                    break;

                case "binary":
                    renderedMessage = FiggleFonts.Binary.Render(message);
                    break;

                case "block":
                    renderedMessage = FiggleFonts.Block.Render(message);
                    break;

                case "blocks":
                    renderedMessage = FiggleFonts.Blocks.Render(message);
                    break;

                case "bolger":
                    renderedMessage = FiggleFonts.Bolger.Render(message);
                    break;

                case "braced":
                    renderedMessage = FiggleFonts.Braced.Render(message);
                    break;

                case "bright":
                    renderedMessage = FiggleFonts.Bright.Render(message);
                    break;

                case "broadway":
                    renderedMessage = FiggleFonts.Broadway.Render(message);
                    break;

                case "broadway_kb":
                    renderedMessage = FiggleFonts.BroadwayKB.Render(message);
                    break;

                case "bubble":
                    renderedMessage = FiggleFonts.Bubble.Render(message);
                    break;

                case "bulbhead":
                    renderedMessage = FiggleFonts.Bulbhead.Render(message);
                    break;

                case "calgphy2":
                    renderedMessage = FiggleFonts.Caligraphy2.Render(message);
                    break;

                case "caligraphy":
                    renderedMessage = FiggleFonts.Caligraphy.Render(message);
                    break;

                case "cards":
                    renderedMessage = FiggleFonts.Cards.Render(message);
                    break;

                case "catwalk":
                    renderedMessage = FiggleFonts.CatWalk.Render(message);
                    break;

                case "chiseled":
                    renderedMessage = FiggleFonts.Chiseled.Render(message);
                    break;

                case "chunky":
                    renderedMessage = FiggleFonts.Chunky.Render(message);
                    break;

                case "coinstak":
                    renderedMessage = FiggleFonts.Coinstak.Render(message);
                    break;

                case "cola":
                    renderedMessage = FiggleFonts.Cola.Render(message);
                    break;

                case "colossal":
                    renderedMessage = FiggleFonts.Colossal.Render(message);
                    break;

                case "computer":
                    renderedMessage = FiggleFonts.Computer.Render(message);
                    break;

                case "contessa":
                    renderedMessage = FiggleFonts.Contessa.Render(message);
                    break;

                case "contrast":
                    renderedMessage = FiggleFonts.Contrast.Render(message);
                    break;

                case "cosmic":
                    renderedMessage = FiggleFonts.Cosmic.Render(message);
                    break;

                case "cosmike":
                    renderedMessage = FiggleFonts.Cosmike.Render(message);
                    break;

                case "crawford":
                    renderedMessage = FiggleFonts.Crawford.Render(message);
                    break;

                case "crazy":
                    renderedMessage = FiggleFonts.Crazy.Render(message);
                    break;

                case "cricket":
                    renderedMessage = FiggleFonts.Cricket.Render(message);
                    break;

                case "cursive":
                    renderedMessage = FiggleFonts.Cursive.Render(message);
                    break;

                case "cyberlarge":
                    renderedMessage = FiggleFonts.CyberLarge.Render(message);
                    break;

                case "cybermedium":
                    renderedMessage = FiggleFonts.CyberMedium.Render(message);
                    break;

                case "cybersmall":
                    renderedMessage = FiggleFonts.CyberSmall.Render(message);
                    break;

                case "cygnet":
                    renderedMessage = FiggleFonts.Cygnet.Render(message);
                    break;

                case "DANC4":
                    renderedMessage = FiggleFonts.DANC4.Render(message);
                    break;

                case "dancingfont":
                    renderedMessage = FiggleFonts.DancingFont.Render(message);
                    break;

                case "decimal":
                    renderedMessage = FiggleFonts.Decimal.Render(message);
                    break;

                case "defleppard":
                    renderedMessage = FiggleFonts.DefLeppard.Render(message);
                    break;

                case "diamond":
                    renderedMessage = FiggleFonts.Diamond.Render(message);
                    break;

                case "dietcola":
                    renderedMessage = FiggleFonts.DietCola.Render(message);
                    break;

                case "digital":
                    renderedMessage = FiggleFonts.Digital.Render(message);
                    break;

                case "doh":
                    renderedMessage = FiggleFonts.Doh.Render(message);
                    break;

                case "doom":
                    renderedMessage = FiggleFonts.Doom.Render(message);
                    break;

                case "dosrebel":
                    renderedMessage = FiggleFonts.DosRebel.Render(message);
                    break;

                case "dotmatrix":
                    renderedMessage = FiggleFonts.DotMatrix.Render(message);
                    break;

                case "double":
                    renderedMessage = FiggleFonts.Double.Render(message);
                    break;

                case "doubleshorts":
                    renderedMessage = FiggleFonts.DoubleShorts.Render(message);
                    break;

                case "drpepper":
                    renderedMessage = FiggleFonts.DRPepper.Render(message);
                    break;

                case "dwhistled":
                    renderedMessage = FiggleFonts.DWhistled.Render(message);
                    break;

                case "eftichess":
                    renderedMessage = FiggleFonts.EftiChess.Render(message);
                    break;

                case "eftifont":
                    renderedMessage = FiggleFonts.EftiFont.Render(message);
                    break;

                case "eftipiti":
                    renderedMessage = FiggleFonts.EftiPiti.Render(message);
                    break;

                case "eftirobot":
                    renderedMessage = FiggleFonts.EftiRobot.Render(message);
                    break;

                case "eftitalic":
                    renderedMessage = FiggleFonts.EftiItalic.Render(message);
                    break;

                case "eftiwall":
                    renderedMessage = FiggleFonts.EftiWall.Render(message);
                    break;

                case "eftiwater":
                    renderedMessage = FiggleFonts.EftiWater.Render(message);
                    break;

                case "epic":
                    renderedMessage = FiggleFonts.Epic.Render(message);
                    break;

                case "fender":
                    renderedMessage = FiggleFonts.Fender.Render(message);
                    break;

                case "filter":
                    renderedMessage = FiggleFonts.Filter.Render(message);
                    break;

                case "fire_font - k":
                    renderedMessage = FiggleFonts.FireFontK.Render(message);
                    break;

                case "fire_font - s":
                    renderedMessage = FiggleFonts.FireFontS.Render(message);
                    break;

                case "flipped":
                    renderedMessage = FiggleFonts.Flipped.Render(message);
                    break;

                case "flowerpower":
                    renderedMessage = FiggleFonts.FlowerPower.Render(message);
                    break;

                case "fourtops":
                    renderedMessage = FiggleFonts.FourTops.Render(message);
                    break;

                case "fraktur":
                    renderedMessage = FiggleFonts.Fraktur.Render(message);
                    break;

                case "funface":
                    renderedMessage = FiggleFonts.FunFace.Render(message);
                    break;

                case "funfaces":
                    renderedMessage = FiggleFonts.FunFaces.Render(message);
                    break;

                case "fuzzy":
                    renderedMessage = FiggleFonts.Fuzzy.Render(message);
                    break;

                case "georgi16":
                    renderedMessage = FiggleFonts.Georgia16.Render(message);
                    break;

                case "Georgia11":
                    renderedMessage = FiggleFonts.Georgia11.Render(message);
                    break;

                case "ghost":
                    renderedMessage = FiggleFonts.Ghost.Render(message);
                    break;

                case "ghoulish":
                    renderedMessage = FiggleFonts.Ghoulish.Render(message);
                    break;

                case "glenyn":
                    renderedMessage = FiggleFonts.Glenyn.Render(message);
                    break;

                case "goofy":
                    renderedMessage = FiggleFonts.Goofy.Render(message);
                    break;

                case "gothic":
                    renderedMessage = FiggleFonts.Gothic.Render(message);
                    break;

                case "graceful":
                    renderedMessage = FiggleFonts.Graceful.Render(message);
                    break;

                case "gradient":
                    renderedMessage = FiggleFonts.Gradient.Render(message);
                    break;

                case "graffiti":
                    renderedMessage = FiggleFonts.Graffiti.Render(message);
                    break;

                case "greek":
                    renderedMessage = FiggleFonts.Greek.Render(message);
                    break;

                case "heart_left":
                    renderedMessage = FiggleFonts.HeartLeft.Render(message);
                    break;

                case "heart_right":
                    renderedMessage = FiggleFonts.HeartRight.Render(message);
                    break;

                case "henry3d":
                    renderedMessage = FiggleFonts.Henry3d.Render(message);
                    break;

                case "hex":
                    renderedMessage = FiggleFonts.Hex.Render(message);
                    break;

                case "hieroglyphs":
                    renderedMessage = FiggleFonts.Hieroglyphs.Render(message);
                    break;

                case "hollywood":
                    renderedMessage = FiggleFonts.Hollywood.Render(message);
                    break;

                case "horizontalleft":
                    renderedMessage = FiggleFonts.HorizontalLeft.Render(message);
                    break;

                case "horizontalright":
                    renderedMessage = FiggleFonts.HorizontalRight.Render(message);
                    break;

                case "ICL - 1900":
                    renderedMessage = FiggleFonts.ICL1900.Render(message);
                    break;

                case "impossible":
                    renderedMessage = FiggleFonts.Impossible.Render(message);
                    break;

                case "invita":
                    renderedMessage = FiggleFonts.Invita.Render(message);
                    break;

                case "isometric1":
                    renderedMessage = FiggleFonts.Isometric1.Render(message);
                    break;

                case "isometric2":
                    renderedMessage = FiggleFonts.Isometric2.Render(message);
                    break;

                case "isometric3":
                    renderedMessage = FiggleFonts.Isometric3.Render(message);
                    break;

                case "isometric4":
                    renderedMessage = FiggleFonts.Isometric4.Render(message);
                    break;

                case "italic":
                    renderedMessage = FiggleFonts.Italic.Render(message);
                    break;

                case "ivrit":
                    renderedMessage = FiggleFonts.Ivrit.Render(message);
                    break;

                case "jacky":
                    renderedMessage = FiggleFonts.Jacky.Render(message);
                    break;

                case "jazmine":
                    renderedMessage = FiggleFonts.Jazmine.Render(message);
                    break;

                case "jerusalem":
                    renderedMessage = FiggleFonts.Jerusalem.Render(message);
                    break;

                case "katakana":
                    renderedMessage = FiggleFonts.Katakana.Render(message);
                    break;

                case "kban":
                    renderedMessage = FiggleFonts.Kban.Render(message);
                    break;

                case "keyboard":
                    renderedMessage = FiggleFonts.Keyboard.Render(message);
                    break;

                case "knob":
                    renderedMessage = FiggleFonts.Knob.Render(message);
                    break;

                case "konto":
                    renderedMessage = FiggleFonts.Konto.Render(message);
                    break;

                case "kontoslant":
                    renderedMessage = FiggleFonts.KontoSlant.Render(message);
                    break;

                case "larry3d":
                    renderedMessage = FiggleFonts.Larry3d.Render(message);
                    break;

                case "lcd":
                    renderedMessage = FiggleFonts.Lcd.Render(message);
                    break;

                case "lean":
                    renderedMessage = FiggleFonts.Lean.Render(message);
                    break;

                case "letters":
                    renderedMessage = FiggleFonts.Letters.Render(message);
                    break;

                case "lildevil":
                    renderedMessage = FiggleFonts.LilDevil.Render(message);
                    break;

                case "lineblocks":
                    renderedMessage = FiggleFonts.LineBlocks.Render(message);
                    break;

                case "linux":
                    renderedMessage = FiggleFonts.Linux.Render(message);
                    break;

                case "lockergnome":
                    renderedMessage = FiggleFonts.LockerGnome.Render(message);
                    break;

                case "madrid":
                    renderedMessage = FiggleFonts.Madrid.Render(message);
                    break;

                case "marquee":
                    renderedMessage = FiggleFonts.Marquee.Render(message);
                    break;

                case "maxfour":
                    renderedMessage = FiggleFonts.MaxFour.Render(message);
                    break;

                case "merlin1":
                    renderedMessage = FiggleFonts.Merlin1.Render(message);
                    break;

                case "merlin2":
                    renderedMessage = FiggleFonts.Merlin2.Render(message);
                    break;

                case "mike":
                    renderedMessage = FiggleFonts.Mike.Render(message);
                    break;

                case "mini":
                    renderedMessage = FiggleFonts.Mini.Render(message);
                    break;

                case "mirror":
                    renderedMessage = FiggleFonts.Mirror.Render(message);
                    break;

                case "mnemonic":
                    renderedMessage = FiggleFonts.Mnemonic.Render(message);
                    break;

                case "modular":
                    renderedMessage = FiggleFonts.Modular.Render(message);
                    break;

                case "morse":
                    renderedMessage = FiggleFonts.Morse.Render(message);
                    break;

                case "morse2":
                    renderedMessage = FiggleFonts.Morse2.Render(message);
                    break;

                case "moscow":
                    renderedMessage = FiggleFonts.Moscow.Render(message);
                    break;

                case "mshebrew210":
                    renderedMessage = FiggleFonts.Mshebrew210.Render(message);
                    break;

                case "muzzle":
                    renderedMessage = FiggleFonts.Muzzle.Render(message);
                    break;

                case "nancyj":
                    renderedMessage = FiggleFonts.NancyJ.Render(message);
                    break;

                case "nancyj - fancy":
                    renderedMessage = FiggleFonts.NancyJFancy.Render(message);
                    break;

                case "nancyj - improved":
                    renderedMessage = FiggleFonts.NancyJImproved.Render(message);
                    break;

                case "nancyj - underlined":
                    renderedMessage = FiggleFonts.NancyJUnderlined.Render(message);
                    break;

                case "nipples":
                    renderedMessage = FiggleFonts.Nipples.Render(message);
                    break;

                case "nscript":
                    renderedMessage = FiggleFonts.NScript.Render(message);
                    break;

                case "ntgreek":
                    renderedMessage = FiggleFonts.NTGreek.Render(message);
                    break;

                case "nvscript":
                    renderedMessage = FiggleFonts.NVScript.Render(message);
                    break;

                case "o8":
                    renderedMessage = FiggleFonts.O8.Render(message);
                    break;

                case "octal":
                    renderedMessage = FiggleFonts.Octal.Render(message);
                    break;

                case "ogre":
                    renderedMessage = FiggleFonts.Ogre.Render(message);
                    break;

                case "oldbanner":
                    renderedMessage = FiggleFonts.OldBanner.Render(message);
                    break;

                case "os2":
                    renderedMessage = FiggleFonts.OS2.Render(message);
                    break;

                case "pawp":
                    renderedMessage = FiggleFonts.Pawp.Render(message);
                    break;

                case "peaks":
                    renderedMessage = FiggleFonts.Peaks.Render(message);
                    break;

                case "peaksslant":
                    renderedMessage = FiggleFonts.PeaksSlant.Render(message);
                    break;

                case "pebbles":
                    renderedMessage = FiggleFonts.Pebbles.Render(message);
                    break;

                case "pepper":
                    renderedMessage = FiggleFonts.Pepper.Render(message);
                    break;

                case "poison":
                    renderedMessage = FiggleFonts.Poison.Render(message);
                    break;

                case "puffy":
                    renderedMessage = FiggleFonts.Puffy.Render(message);
                    break;

                case "puzzle":
                    renderedMessage = FiggleFonts.Puzzle.Render(message);
                    break;

                case "pyramid":
                    renderedMessage = FiggleFonts.Pyramid.Render(message);
                    break;

                case "rammstein":
                    renderedMessage = FiggleFonts.Rammstein.Render(message);
                    break;

                case "rectangles":
                    renderedMessage = FiggleFonts.Rectangles.Render(message);
                    break;

                case "red_phoenix":
                    renderedMessage = FiggleFonts.RedPhoenix.Render(message);
                    break;

                case "relief":
                    renderedMessage = FiggleFonts.Relief.Render(message);
                    break;

                case "relief2":
                    renderedMessage = FiggleFonts.Relief2.Render(message);
                    break;

                case "rev":
                    renderedMessage = FiggleFonts.Rev.Render(message);
                    break;

                case "reverse":
                    renderedMessage = FiggleFonts.Reverse.Render(message);
                    break;

                case "roman":
                    renderedMessage = FiggleFonts.Roman.Render(message);
                    break;

                case "rot13":
                    renderedMessage = FiggleFonts.Rot13.Render(message);
                    break;

                case "rotated":
                    renderedMessage = FiggleFonts.Rotated.Render(message);
                    break;

                case "rounded":
                    renderedMessage = FiggleFonts.Rounded.Render(message);
                    break;

                case "rowancap":
                    renderedMessage = FiggleFonts.RowanCap.Render(message);
                    break;

                case "rozzo":
                    renderedMessage = FiggleFonts.Rozzo.Render(message);
                    break;

                case "runic":
                    renderedMessage = FiggleFonts.Runic.Render(message);
                    break;

                case "runyc":
                    renderedMessage = FiggleFonts.Runyc.Render(message);
                    break;

                case "santaclara":
                    renderedMessage = FiggleFonts.SantaClara.Render(message);
                    break;

                case "sblood":
                    renderedMessage = FiggleFonts.SBlood.Render(message);
                    break;

                case "script":
                    renderedMessage = FiggleFonts.Script.Render(message);
                    break;

                case "slscript":
                    renderedMessage = FiggleFonts.ScriptSlant.Render(message);
                    break;

                case "serifcap":
                    renderedMessage = FiggleFonts.SerifCap.Render(message);
                    break;

                case "shadow":
                    renderedMessage = FiggleFonts.Shadow.Render(message);
                    break;

                case "shimrod":
                    renderedMessage = FiggleFonts.Shimrod.Render(message);
                    break;

                case "short":
                    renderedMessage = FiggleFonts.Short.Render(message);
                    break;

                case "slant":
                    renderedMessage = FiggleFonts.Slant.Render(message);
                    break;

                case "slide":
                    renderedMessage = FiggleFonts.Slide.Render(message);
                    break;

                case "small":
                    renderedMessage = FiggleFonts.Small.Render(message);
                    break;

                case "smallcaps":
                    renderedMessage = FiggleFonts.SmallCaps.Render(message);
                    break;

                case "smisome1":
                    renderedMessage = FiggleFonts.IsometricSmall.Render(message);
                    break;

                case "smkeyboard":
                    renderedMessage = FiggleFonts.KeyboardSmall.Render(message);
                    break;

                case "smpoison":
                    renderedMessage = FiggleFonts.PoisonSmall.Render(message);
                    break;

                case "smscript":
                    renderedMessage = FiggleFonts.ScriptSmall.Render(message);
                    break;

                case "smshadow":
                    renderedMessage = FiggleFonts.ShadowSmall.Render(message);
                    break;

                case "smslant":
                    renderedMessage = FiggleFonts.SlantSmall.Render(message);
                    break;

                case "smtengwar":
                    renderedMessage = FiggleFonts.TengwarSmall.Render(message);
                    break;

                case "soft":
                    renderedMessage = FiggleFonts.Soft.Render(message);
                    break;

                case "speed":
                    renderedMessage = FiggleFonts.Speed.Render(message);
                    break;

                case "spliff":
                    renderedMessage = FiggleFonts.Spliff.Render(message);
                    break;

                case "s - relief":
                    renderedMessage = FiggleFonts.SRelief.Render(message);
                    break;

                case "stacey":
                    renderedMessage = FiggleFonts.Stacey.Render(message);
                    break;

                case "stampate":
                    renderedMessage = FiggleFonts.Stampate.Render(message);
                    break;

                case "stampatello":
                    renderedMessage = FiggleFonts.Stampatello.Render(message);
                    break;

                case "standard":
                    renderedMessage = FiggleFonts.Standard.Render(message);
                    break;

                case "starstrips":
                    renderedMessage = FiggleFonts.Starstrips.Render(message);
                    break;

                case "starwars":
                    renderedMessage = FiggleFonts.Starwars.Render(message);
                    break;

                case "stellar":
                    renderedMessage = FiggleFonts.Stellar.Render(message);
                    break;

                case "stforek":
                    renderedMessage = FiggleFonts.Stforek.Render(message);
                    break;

                case "stop":
                    renderedMessage = FiggleFonts.Stop.Render(message);
                    break;

                case "straight":
                    renderedMessage = FiggleFonts.Straight.Render(message);
                    break;

                case "sub - zero":
                    renderedMessage = FiggleFonts.SubZero.Render(message);
                    break;

                case "swampland":
                    renderedMessage = FiggleFonts.Swampland.Render(message);
                    break;

                case "swan":
                    renderedMessage = FiggleFonts.Swan.Render(message);
                    break;

                case "sweet":
                    renderedMessage = FiggleFonts.Sweet.Render(message);
                    break;

                case "tanja":
                    renderedMessage = FiggleFonts.Tanja.Render(message);
                    break;

                case "tengwar":
                    renderedMessage = FiggleFonts.Tengwar.Render(message);
                    break;

                case "term":
                    renderedMessage = FiggleFonts.Term.Render(message);
                    break;

                case "test1":
                    renderedMessage = FiggleFonts.Test1.Render(message);
                    break;

                case "thick":
                    renderedMessage = FiggleFonts.Thick.Render(message);
                    break;

                case "thin":
                    renderedMessage = FiggleFonts.Thin.Render(message);
                    break;

                case "threepoint":
                    renderedMessage = FiggleFonts.ThreePoint.Render(message);
                    break;

                case "ticks":
                    renderedMessage = FiggleFonts.Ticks.Render(message);
                    break;

                case "ticksslant":
                    renderedMessage = FiggleFonts.TicksSlant.Render(message);
                    break;

                case "tiles":
                    renderedMessage = FiggleFonts.Tiles.Render(message);
                    break;

                case "tinker - toy":
                    renderedMessage = FiggleFonts.TinkerToy.Render(message);
                    break;

                case "tombstone":
                    renderedMessage = FiggleFonts.Tombstone.Render(message);
                    break;

                case "train":
                    renderedMessage = FiggleFonts.Train.Render(message);
                    break;

                case "trek":
                    renderedMessage = FiggleFonts.Trek.Render(message);
                    break;

                case "tsalagi":
                    renderedMessage = FiggleFonts.Tsalagi.Render(message);
                    break;

                case "tubular":
                    renderedMessage = FiggleFonts.Tubular.Render(message);
                    break;

                case "twisted":
                    renderedMessage = FiggleFonts.Twisted.Render(message);
                    break;

                case "twopoint":
                    renderedMessage = FiggleFonts.TwoPoint.Render(message);
                    break;

                case "univers":
                    renderedMessage = FiggleFonts.Univers.Render(message);
                    break;

                case "usaflag":
                    renderedMessage = FiggleFonts.UsaFlag.Render(message);
                    break;

                case "varsity":
                    renderedMessage = FiggleFonts.Varsity.Render(message);
                    break;

                case "wavy":
                    renderedMessage = FiggleFonts.Wavy.Render(message);
                    break;

                case "weird":
                    renderedMessage = FiggleFonts.Weird.Render(message);
                    break;

                case "wetletter":
                    renderedMessage = FiggleFonts.WetLetter.Render(message);
                    break;

                case "whimsy":
                    renderedMessage = FiggleFonts.Whimsy.Render(message);
                    break;

                case "wow":
                    renderedMessage = FiggleFonts.Wow.Render(message);
                    break;

                default:
                    await sktMessage.Channel.SendMessageAsync(
                        $"Unable to render text as the font {pickedFont} does not exist");
                    break;
            }

            if (renderedMessage != null)
            {
                var msg = DiscordRenderedText(renderedMessage);
                if (msg.Length > 1950)
                    await sktMessage.Channel.SendMessageAsync("Sorry, I cannot send this as it's over 2000 characters, and Discord is bad");
                else
                    await sktMessage.Channel.SendMessageAsync(msg);
            }
        }
    }
}