using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LostRealms {
 public static class GameLanguage {
  [Serializable] public sealed class Entry {public string key,english,arabic,context;public int maxCharacters;}
  [Serializable] public sealed class Catalog {public Entry[] entries;}
  public const string PreferenceKey="LostRealms.Language";
  static readonly Dictionary<string,string> words=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
  static readonly Dictionary<string,string> translated=new Dictionary<string,string>();
  static readonly Dictionary<string,string> shaped=new Dictionary<string,string>();
  sealed class Layout {public int fontSize;public string visual;}
  static readonly Dictionary<string,Layout> layouts=new Dictionary<string,Layout>();
  static readonly List<Template> templates=new List<Template>();
  sealed class Template {public Regex pattern;public string arabic;}
  static Regex phrases;static bool initialized,arabic;static Font font,displayFont;
  public static int Version {get;private set;}
  public static bool IsArabic {get{Initialize();return arabic;}}
  public static Font ArabicFont {get{if(!font)font=Resources.Load<Font>("UI/Fonts/NotoSansArabicUI-Regular");return font;}}
  public static Font ArabicDisplayFont {get{if(!displayFont)displayFont=Resources.Load<Font>("UI/Fonts/LostRealmsArabicDisplay");return displayFont?displayFont:ArabicFont;}}
  public static Entry[] Entries {get;private set;}
  public static readonly Dictionary<string,string> ObservedText=new Dictionary<string,string>();
  public static void Initialize(){
   if(initialized)return;initialized=true;
   arabic=PlayerPrefs.GetString(PreferenceKey,"en")=="ar";
   var asset=Resources.Load<TextAsset>("Localization/GameText");
   Entries=asset?JsonUtility.FromJson<Catalog>(asset.text).entries:Array.Empty<Entry>();
   foreach(var entry in Entries){
    words[entry.english]=entry.arabic;
    if(Regex.IsMatch(entry.english,@"\{\d+\}")){
     var pattern=new StringBuilder("^");int offset=0;
     foreach(Match placeholder in Regex.Matches(entry.english,@"\{(\d+)\}")){
      pattern.Append(Regex.Escape(entry.english.Substring(offset,placeholder.Index-offset)));
      string capture=entry.english=="{0}  {1}s"&&placeholder.Groups[1].Value=="1"?@"[0-9]+(?:[.,][0-9]+)?":".*?";
      pattern.Append("(?<p"+placeholder.Groups[1].Value+">"+capture+")");offset=placeholder.Index+placeholder.Length;
     }
     pattern.Append(Regex.Escape(entry.english.Substring(offset))).Append('$');
     templates.Add(new Template{pattern=new Regex(pattern.ToString(),RegexOptions.IgnoreCase|RegexOptions.Singleline),arabic=entry.arabic});
    }
   }
   templates.Sort((a,b)=>b.pattern.ToString().Length.CompareTo(a.pattern.ToString().Length));
   var keys=new List<string>(words.Keys);keys.RemoveAll(k=>Regex.IsMatch(k,@"\{\d+\}"));keys.Sort((a,b)=>b.Length.CompareTo(a.Length));
   phrases=new Regex(string.Join("|",keys.ConvertAll(k=>(char.IsLetter(k[0])?@"(?<![A-Za-z])":"")+Regex.Escape(k)+(char.IsLetter(k[k.Length-1])?@"(?![A-Za-z])":""))),RegexOptions.IgnoreCase);
  }
  public static void SetArabic(bool enabled,bool persist=true){
   Initialize();if(arabic!=enabled){arabic=enabled;Version++;translated.Clear();shaped.Clear();layouts.Clear();}
   if(persist&&!RealmGame.Testing){PlayerPrefs.SetString(PreferenceKey,enabled?"ar":"en");PlayerPrefs.Save();}
  }
  // Source data stays in English: IDs, save keys, model paths and gameplay comparisons never change.
  public static string Translate(string source){
   if(string.IsNullOrEmpty(source)||!IsArabic)return source;
   if(translated.TryGetValue(source,out var cached))return cached;
   string value=TranslateCore(source,0);
   if(RealmGame.Testing)ObservedText[source]=value;
   if(translated.Count>4096)translated.Clear();translated[source]=value;return value;
  }
  static string TranslateCore(string source,int depth){
   if(words.TryGetValue(source,out var exact)&&!Regex.IsMatch(source,@"\{\d+\}"))return exact;
   if(depth<3)foreach(var template in templates){
    var match=template.pattern.Match(source);if(!match.Success)continue;
    return Regex.Replace(template.arabic,@"\{(\d+)\}",m=>TranslateCore(match.Groups["p"+m.Groups[1].Value].Value,depth+1));
   }
   return phrases.Replace(source,m=>words[m.Value]);
  }
  public static bool ContainsArabic(string value)=>!string.IsNullOrEmpty(value)&&Regex.IsMatch(value,@"[\u0600-\u06ff\ufb50-\ufeff]");
  public static string Shape(string logical){
   if(!ContainsArabic(logical))return logical;
   if(shaped.TryGetValue(logical,out var result))return result;
   result=ShapeProtected(logical);
   if(shaped.Count>4096)shaped.Clear();shaped[logical]=result;return result;
  }
  static string ShapeProtected(string logical){
   if(!ContainsArabic(logical))return logical;
   var protectedRuns=new Dictionary<string,string>();int index=0;
   Func<string,string> protect=value=>{
    int id=index++;string letters="";do{letters=(char)('A'+id%26)+letters;id=id/26-1;}while(id>=0);
    string marker="ZXQ"+letters+"QXZ";protectedRuns.Add(marker,value);return marker;
   };
   var text=new StringBuilder();
   for(int i=0;i<logical.Length;i++){
    char open=logical[i],close=open=='('?')':open=='['?']':open=='{'?'}':'\0';int end=-1;
    if(close!='\0'){
     int depth=1;for(int j=i+1;j<logical.Length;j++){if(logical[j]==open)depth++;if(logical[j]==close&&--depth==0){end=j;break;}}
    }
    if(end>i){text.Append(protect(open+ShapeProtected(logical.Substring(i+1,end-i-1))+close));i=end;}else text.Append(open);
   }
   // Protect complete LTR numeric runs, including signs, ranges, fractions and percentages.
   string masked=Regex.Replace(text.ToString(),@"[+\-−]?\d+(?:[.,:/\-–]\d+)*(?:%|٪)?",m=>protect(m.Value));
   string visual=ArabicSupport.ArabicFixer.Fix(masked,true,false,false);
   foreach(var run in protectedRuns)visual=visual.Replace(run.Key,run.Value);
   return visual;
  }
  public static GUIStyle Style(GUIStyle original){
   var result=new GUIStyle(original){font=original.fontSize>=19?ArabicDisplayFont:ArabicFont,fontStyle=FontStyle.Normal,richText=false};
   switch(result.alignment){
    case TextAnchor.UpperLeft:result.alignment=TextAnchor.UpperRight;break;
    case TextAnchor.MiddleLeft:result.alignment=TextAnchor.MiddleRight;break;
    case TextAnchor.LowerLeft:result.alignment=TextAnchor.LowerRight;break;
   }
   return result;
  }
  // Wrap logical words first, then shape each visual line. Reversing a whole paragraph before wrapping reverses line order.
  public static string Wrap(string logical,GUIStyle style,float width){
   var result=new StringBuilder();
   foreach(var paragraph in logical.Replace("\r","").Split('\n')){
    string line="";
    bool bullets=paragraph.Contains(" • ");
    var units=bullets?new List<string>(paragraph.Split(new[]{" • "},StringSplitOptions.None)):new List<string>();
    if(!bullets)foreach(Match word in Regex.Matches(paragraph,@"\([^)]*\)|\[[^\]]*\]|\S+"))units.Add(word.Value);
    foreach(var word in units){
     string next=line.Length==0?word:line+(bullets?" • ":" ")+word;
     if(line.Length>0&&style.CalcSize(new GUIContent(Shape(next))).x>width){result.AppendLine(Shape(line));line=word;}else line=next;
    }
    result.AppendLine(Shape(line));
   }
   return result.ToString().TrimEnd('\r','\n');
  }
  public static void Label(Rect rect,string source,GUIStyle original){
   string logical=Translate(source);
   if(!ContainsArabic(logical)){GUI.Label(rect,logical,original);return;}
   var style=Style(original);bool wrap=original.wordWrap&&rect.height>=original.fontSize*1.6f;
   style.wordWrap=false;style.clipping=TextClipping.Clip;
   string key=style.font.name+"|"+logical+"|"+original.fontSize+"|"+rect.width+"|"+rect.height+"|"+wrap+"|"+style.padding.horizontal+"|"+style.padding.vertical;
   if(!layouts.TryGetValue(key,out var layout)){
    int minimum=Math.Min(8,original.fontSize);
    style.fontSize=Math.Min(original.fontSize,Mathf.Max(minimum,Mathf.FloorToInt(rect.height*.7f)));
    string visual;
    do{
     visual=wrap?Wrap(logical,style,rect.width):Shape(logical);
     var size=style.CalcSize(new GUIContent(visual));
     if(size.x<=rect.width+1&&size.y<=rect.height+1||style.fontSize<=minimum)break;
     style.fontSize--;
    }while(true);
    // IMGUI in Unity 6000.6 performs bidi reordering. These lines already have visual order;
    // explicitly retain it, including signed numbers and percentage/currency groups.
    layout=new Layout{fontSize=style.fontSize,visual="\u202D"+visual.Replace("\r","").Replace("\n","\u202C\n\u202D")+"\u202C"};
    if(layouts.Count>2048)layouts.Clear();layouts[key]=layout;
   }
   style.fontSize=layout.fontSize;GUI.Label(rect,layout.visual,style);
  }
  public static void Label(Rect rect,GUIContent content,GUIStyle style)=>Label(rect,content.text,style);
 }
}
