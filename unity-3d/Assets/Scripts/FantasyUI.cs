using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LostRealms {
 // Built once on the game owner, not the disposable chapter root.
 // Gameplay controls are visual-only: TouchRouter remains their sole input owner.
 [DefaultExecutionOrder(100)]
 public sealed class FantasyUI : MonoBehaviour {
  public bool ShowMobileControlsForCapture;
  public bool CoversMenu=>isActiveAndEnabled&&ready&&!game.ArsenalVisible&&(game.Screen==GameScreen.Menu||game.Screen==GameScreen.Paused);
  public bool CoversHud=>isActiveAndEnabled&&ready&&game.Screen==GameScreen.Playing;
  RealmGame game; bool ready;
  RectTransform safe,menu,pause,hud,confirmation,mobile,joystick,handle,moveRegion,cameraRegion;
  readonly RectTransform[] actions=new RectTransform[6];
  readonly Rect[] actionRects=new Rect[6];
  readonly Vector3[] corners=new Vector3[4];
  readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
  TMP_FontAsset heading,body; TMP_FontAsset runtimeHeading,runtimeBody;
  TMP_Text journey,stars,gold,gems,location,pauseInfo,hpText,energyText,chapter,weapon,objectives,timer,compass,combat,element;
  Image hpFill,energyFill; RawImage portrait; FantasyMapGraphic map;
  RectTransform pauseHit,elementHit;
  RectTransform noticeSlot,trialSlot;
  GameObject canvasObject,ownedEvents;
  RenderTexture heroPortrait;
  bool portraitAttempted;
  TMP_Text portraitFallback;
  Rect lastSafe; int lastWidth,lastHeight; bool lastMobile;
  static readonly Color Ivory=new Color(.96f,.92f,.81f),Gold=new Color(.84f,.70f,.43f),Ice=new Color(.52f,.85f,1);

  public void Initialize(RealmGame owner){
   game=owner;
   try{Build();}
   catch(Exception error){
    ready=false;enabled=false;
    if(canvasObject){canvasObject.SetActive(false);Destroy(canvasObject);}
    if(ownedEvents){ownedEvents.SetActive(false);Destroy(ownedEvents);}
    TouchRouter.ClearLayout();
    Debug.LogError("FantasyUI initialization failed; legacy menus/HUD remain enabled. "+error);
   }
  }
  void Build(){
   ShowMobileControlsForCapture=Array.IndexOf(Environment.GetCommandLineArgs(),"-fantasyUiCapture")>=0;
   heading=LoadFont("Heading",out runtimeHeading);body=LoadFont("Body",out runtimeBody);
   if(!heading||!body){Debug.LogError("FantasyUI: Heading and Body fonts are required. Legacy UI remains enabled.");enabled=false;return;}
   canvasObject=new GameObject("Fantasy UI • Screen Overlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
   canvasObject.transform.SetParent(transform,false);
   var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
   var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=1;
   if(!FindFirstObjectByType<EventSystem>()){
    ownedEvents=new GameObject("Fantasy UI EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));ownedEvents.transform.SetParent(transform,false);
   }
   safe=Node("Device safe area",canvasObject.transform);Stretch(safe);
   BuildMenu();BuildPause();BuildHud();BuildConfirmation();
   ready=true;RefreshLayout();Refresh();
  }
  TMP_FontAsset LoadFont(string name,out TMP_FontAsset generated){
   generated=null;var font=Resources.Load<TMP_FontAsset>("FantasyUI/Fonts/"+name+" SDF");if(font)return font;
   var source=Resources.Load<Font>("FantasyUI/Fonts/"+name);
   if(!source){Debug.LogError("FantasyUI: missing Resources/FantasyUI/Fonts/"+name+" SDF and source "+name+".");return null;}
   generated=TMP_FontAsset.CreateFontAsset(source);
   if(!generated)Debug.LogError("FantasyUI: could not create runtime TMP font "+name+".");
   else Debug.LogWarning("FantasyUI: generated runtime "+name+" TMP font; run the editor font baker for a prebuilt atlas.");
   return generated;
  }
  Sprite Sprite(string name){
   if(sprites.TryGetValue(name,out var sprite))return sprite;
   sprite=Resources.Load<Sprite>("FantasyUI/"+name);sprites.Add(name,sprite);
   if(!sprite)Debug.LogError("FantasyUI: missing sprite Resources/FantasyUI/"+name+". Check the UI kit importer.");
   return sprite;
  }
  RectTransform Node(string name,Transform parent){
   var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;
  }
  static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;}
  static void Place(RectTransform r,Vector2 anchor,Vector2 position,Vector2 size){
   r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;
  }
  Image Art(string name,Transform parent,string sprite,Vector2 anchor,Vector2 position,Vector2 size,bool sliced=false){
   var r=Node(name,parent);Place(r,anchor,position,size);var image=r.gameObject.AddComponent<Image>();
   image.sprite=Sprite(sprite);image.type=sliced?Image.Type.Sliced:Image.Type.Simple;image.raycastTarget=false;return image;
  }
  TMP_Text Text(string name,Transform parent,string value,float size,Vector2 anchor,Vector2 position,Vector2 bounds,bool title=false){
   var r=Node(name,parent);Place(r,anchor,position,bounds);var t=r.gameObject.AddComponent<TextMeshProUGUI>();
   t.font=title?heading:body;t.fontSize=size;t.color=Ivory;t.alignment=TextAlignmentOptions.Center;t.text=value;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;
   return t;
  }
  static void Set(TMP_Text text,string value){if(text.text!=value)text.text=value;}
  RectTransform Panel(string name,Transform parent,Vector2 size){
   return Art(name,parent,"panel",new Vector2(.5f,.5f),Vector2.zero,size,true).rectTransform;
  }
  Button Button(Transform parent,string label,string icon,UnityAction callback,bool primary=false){
   var image=Art(label,parent,primary?"button-active":"button",new Vector2(.5f,.5f),Vector2.zero,new Vector2(420,62),true);image.raycastTarget=true;
   var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.SpriteSwap;
   button.spriteState=new SpriteState{highlightedSprite=Sprite("button-active"),pressedSprite=Sprite("button-active"),selectedSprite=Sprite("button-active")};
   button.onClick.AddListener(callback);
   Art(label+" icon",image.transform,icon,new Vector2(0,.5f),new Vector2(50,0),new Vector2(30,30));
   Text(label+" label",image.transform,label,23,new Vector2(.5f,.5f),new Vector2(20,0),new Vector2(330,48),true);
   var layout=image.gameObject.AddComponent<LayoutElement>();layout.preferredHeight=62;layout.minHeight=62;
   return button;
  }
  RectTransform Stack(Transform parent,Vector2 pos,Vector2 size){
   var r=Node("Actions",parent);Place(r,new Vector2(.5f,.5f),pos,size);
   var layout=r.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=9;layout.childAlignment=TextAnchor.MiddleCenter;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;
   return r;
  }
  void BuildMenu(){
   menu=Panel("Main menu • live realm backdrop",safe,new Vector2(560,674));
   Art("Royal crest",menu,"crest",new Vector2(.5f,1),new Vector2(0,-96),new Vector2(228,184));
   Text("Eyebrow",menu,"THE FOUR REALMS AWAIT",13,new Vector2(.5f,1),new Vector2(0,-27),new Vector2(430,24));
   var title=Text("Game title",menu,"LEGENDS\n<size=22>OF THE</size>\nLOST REALMS",44,new Vector2(.5f,1),new Vector2(0,-125),new Vector2(500,174),true);title.color=Gold;title.lineSpacing=-12;
   Art("Gold divider",menu,"divider",new Vector2(.5f,1),new Vector2(0,-224),new Vector2(442,16));
   var stats=Node("Journey statistics",menu);Place(stats,new Vector2(.5f,1),new Vector2(0,-266),new Vector2(464,62));
   var row=stats.gameObject.AddComponent<HorizontalLayoutGroup>();row.childControlWidth=true;row.childForceExpandWidth=true;row.childControlHeight=true;row.spacing=2;
   journey=Stat(stats,"icon-atlas","CHAPTER");stars=Stat(stats,"icon-star","STARS");gold=Stat(stats,"icon-coin","GOLD");gems=Stat(stats,"icon-gem","GEMS");
   // Content ends at panel y=589.5; location begins at y=605 (15.5px gap).
   // Uniform safe-area scaling preserves this gap at every aspect ratio.
   var buttons=Stack(menu,new Vector2(0,-115),new Vector2(430,279));
   Button(buttons,"CONTINUE JOURNEY","icon-play",()=>game.LoadLevel(game.Save.unlocked),true);
   Button(buttons,"REALM ATLAS","icon-atlas",()=>game.Screen=GameScreen.Map);
   Button(buttons,"SANCTUARY","icon-sanctuary",()=>game.Screen=GameScreen.Settings);
   Button(buttons,"NEW JOURNEY","icon-new",()=>confirmation.gameObject.SetActive(true));
   location=Text("Current journey",menu,"",15,new Vector2(.5f,0),new Vector2(0,53),new Vector2(480,32));
   Text("Autosave",menu,"TOUCH + KEYBOARD   •   JOURNEY AUTOSAVES",11,new Vector2(.5f,0),new Vector2(0,25),new Vector2(470,22));
  }
  TMP_Text Stat(Transform parent,string icon,string label){
   var r=Node(label,parent);Art(label+" icon",r,icon,new Vector2(.5f,1),new Vector2(0,-12),new Vector2(24,24));
   var value=Text(label+" value",r,"0",18,new Vector2(.5f,.5f),new Vector2(0,-4),new Vector2(110,24),true);
   Text(label+" caption",r,label,10,new Vector2(.5f,0),new Vector2(0,4),new Vector2(110,16));return value;
  }
  void BuildPause(){
   pause=Panel("Pause menu",safe,new Vector2(560,574));
   Art("Pause crest",pause,"crest",new Vector2(.5f,1),new Vector2(0,-60),new Vector2(132,106));
   Text("Pause eyebrow",pause,"JOURNEY PAUSED",13,new Vector2(.5f,1),new Vector2(0,-112),new Vector2(440,22));
   Text("Pause title",pause,"A Moment of Rest",36,new Vector2(.5f,1),new Vector2(0,-151),new Vector2(480,56),true).color=Gold;
   Art("Pause divider",pause,"divider",new Vector2(.5f,1),new Vector2(0,-191),new Vector2(440,16));
   pauseInfo=Text("Chapter and time",pause,"",16,new Vector2(.5f,1),new Vector2(0,-224),new Vector2(460,38));
   var buttons=Stack(pause,new Vector2(0,-115),new Vector2(430,280));
   Button(buttons,"RESUME JOURNEY","icon-play",game.Resume,true);
   Button(buttons,"ARSENAL","icon-blade",game.OpenArsenal);
   Button(buttons,"REALM ATLAS","icon-atlas",()=>game.Screen=GameScreen.Map);
   Button(buttons,"RESTART CHAPTER","icon-restart",()=>game.LoadLevel(game.Level));
  }
  void BuildConfirmation(){
   confirmation=Node("Confirm new journey",safe);Stretch(confirmation);
   // Explicit modal blocker prevents accidental clicks through to Continue.
   var blocker=confirmation.gameObject.AddComponent<Image>();blocker.color=new Color(.015f,.025f,.045f,.88f);blocker.raycastTarget=true;
   var panel=Panel("Reset confirmation",confirmation,new Vector2(530,340));
   Text("Reset heading",panel,"Begin Again?",34,new Vector2(.5f,1),new Vector2(0,-60),new Vector2(450,54),true).color=Gold;
   Text("Reset warning",panel,"This erases your saved chapters, treasury,\nweapons and upgrades. This cannot be undone.",19,new Vector2(.5f,1),new Vector2(0,-129),new Vector2(456,74));
   var buttons=Stack(panel,new Vector2(0,-78),new Vector2(414,133));
   Button(buttons,"KEEP MY JOURNEY","icon-play",()=>DismissConfirmation(),true);
   Button(buttons,"ERASE & BEGIN","icon-new",()=>{DismissConfirmation();game.ResetJourney();});
   confirmation.gameObject.SetActive(false);
  }
  public bool DismissConfirmation(){if(!confirmation||!confirmation.gameObject.activeSelf)return false;confirmation.gameObject.SetActive(false);return true;}
  void BuildHud(){
   hud=Node("Gameplay HUD",safe);Stretch(hud);
   var vitals=Art("Vitals",hud,"panel",new Vector2(0,1),new Vector2(180,-79),new Vector2(340,136),true).rectTransform;
   var portraitRoot=Art("Aster portrait frame",vitals,"portrait-frame",new Vector2(0,.5f),new Vector2(58,0),new Vector2(100,112)).rectTransform;
   var pr=Node("Genuine Aster portrait",portraitRoot);Place(pr,new Vector2(.5f,.5f),new Vector2(0,6),new Vector2(69,75));portrait=pr.gameObject.AddComponent<RawImage>();portrait.raycastTarget=false;portrait.enabled=false;
   portraitFallback=Text("Portrait fallback",portraitRoot,"ASTER",10,new Vector2(.5f,.5f),new Vector2(0,6),new Vector2(74,45));
   chapter=Text("Chapter badge",portraitRoot,"",13,new Vector2(.5f,0),new Vector2(0,14),new Vector2(80,24),true);
   weapon=Text("Equipped weapon",vitals,"",17,new Vector2(0,1),new Vector2(221,-27),new Vector2(214,26),true);
   weapon.enableAutoSizing=true;weapon.fontSizeMin=11;weapon.fontSizeMax=17;weapon.textWrappingMode=TextWrappingModes.NoWrap;
   hpFill=Bar(vitals,"Vitality",new Vector2(223,9),new Color(.69f,.16f,.20f));
   energyFill=Bar(vitals,"Aether",new Vector2(223,-25),new Color(.20f,.58f,.85f));
   hpText=Text("Health amount",vitals,"",13,new Vector2(0,.5f),new Vector2(223,10),new Vector2(194,23));
   energyText=Text("Aether amount",vitals,"",12,new Vector2(0,.5f),new Vector2(223,-24),new Vector2(194,23));
   var objectivePanel=Art("Objective frame",hud,"panel",new Vector2(0,1),new Vector2(180,-190),new Vector2(340,72),true).rectTransform;
   objectives=Text("Live objectives",objectivePanel,"",15,new Vector2(.5f,.5f),Vector2.zero,new Vector2(310,60));
   combat=Text("Combat response",hud,"",16,new Vector2(0,1),new Vector2(180,-247),new Vector2(338,27));combat.color=Gold;
   var power=Art("Element selection",hud,"button",new Vector2(0,1),new Vector2(180,-425),new Vector2(236,42),true);power.raycastTarget=true;
   elementHit=power.rectTransform;
   var powerButton=power.gameObject.AddComponent<Button>();powerButton.targetGraphic=power;powerButton.onClick.AddListener(game.CycleElement);
   element=Text("Element",power.transform,"",15,new Vector2(.5f,.5f),Vector2.zero,new Vector2(220,34));
   var timeFrame=Art("Time frame",hud,"panel",new Vector2(1,1),new Vector2(-174,-32),new Vector2(156,46),true);
   timer=Text("Elapsed time",timeFrame.transform,"",19,new Vector2(.5f,.5f),Vector2.zero,new Vector2(138,30));
   var pauseImage=Art("Pause",hud,"round",new Vector2(1,1),new Vector2(-45,-32),new Vector2(52,52));pauseImage.raycastTarget=true;
   pauseHit=pauseImage.rectTransform;
   Art("Pause icon",pauseImage.transform,"icon-pause",new Vector2(.5f,.5f),Vector2.zero,new Vector2(26,26));
   var pauseButton=pauseImage.gameObject.AddComponent<Button>();pauseButton.targetGraphic=pauseImage;pauseButton.onClick.AddListener(game.Pause);
   var mapFrame=Art("Minimap frame",hud,"minimap-frame",new Vector2(1,1),new Vector2(-98,-151),new Vector2(176,176)).rectTransform;
   var clip=Node("Circular map mask",mapFrame);Place(clip,new Vector2(.5f,.5f),Vector2.zero,new Vector2(144,144));
   var disc=clip.gameObject.AddComponent<FantasyDiscGraphic>();disc.color=new Color(.025f,.065f,.09f,.96f);disc.raycastTarget=false;clip.gameObject.AddComponent<Mask>().showMaskGraphic=true;
   var mapNode=Node("Live route and actors",clip);Stretch(mapNode);map=mapNode.gameObject.AddComponent<FantasyMapGraphic>();map.Game=game;map.raycastTarget=false;
   // Frame above the masked live data; no invented geographical artwork.
   Art("Map rim",mapFrame,"minimap-frame",new Vector2(.5f,.5f),Vector2.zero,new Vector2(176,176));
   Text("Map north",mapFrame,"N",12,new Vector2(.5f,1),new Vector2(0,-12),new Vector2(24,22));
   var compassFrame=Art("Route compass",hud,"panel",new Vector2(.5f,1),new Vector2(0,-35),new Vector2(184,52),true);
   compass=Text("Next route bearing",compassFrame.transform,"",14,new Vector2(.5f,.5f),Vector2.zero,new Vector2(174,44));
   mobile=Node("Touch controls",hud);Stretch(mobile);
   joystick=Art("Joystick base",mobile,"joystick-base",Vector2.zero,new Vector2(139,111),new Vector2(172,172)).rectTransform;
   handle=Art("Joystick handle",joystick,"joystick-handle",new Vector2(.5f,.5f),Vector2.zero,new Vector2(68,68)).rectTransform;
   Text("Move caption",joystick,"MOVE",11,new Vector2(.5f,0),new Vector2(0,-3),new Vector2(100,18));
   string[] labels={"DODGE","POWER","BLADE","JUMP","PARRY","SPELL"};
   string[] hints={"","ELEMENT","HOLD TO CHARGE","DOUBLE JUMP","","HOLD TO LOB"};
   for(int i=0;i<6;i++){
    var r=TouchRouter.DefaultActionRect(i);
    actions[i]=Art(labels[i],mobile,i==2?"round-primary":"round",new Vector2(1,0),new Vector2(r.center.x-1280,720-r.center.y),r.size).rectTransform;
    Art(labels[i]+" glyph",actions[i],"icon-"+labels[i].ToLowerInvariant(),new Vector2(.5f,.5f),new Vector2(0,i==2?13:8),Vector2.one*r.width*.39f);
    Text(labels[i]+" caption",actions[i],labels[i],i==2?17:11,new Vector2(.5f,.5f),new Vector2(0,-r.height*.22f),new Vector2(r.width-8,23),true);
    if(i==2)Text("Charge hint",actions[i],hints[i],8,new Vector2(.5f,.5f),new Vector2(0,-51),new Vector2(136,16));
   }
   moveRegion=Node("Move touch region",mobile);Place(moveRegion,Vector2.zero,new Vector2(205,156),new Vector2(410,312));
   cameraRegion=Node("Camera touch region",hud);cameraRegion.anchorMin=new Vector2(.34f,.29f);cameraRegion.anchorMax=new Vector2(.98f,.72f);cameraRegion.offsetMin=cameraRegion.offsetMax=Vector2.zero;
   noticeSlot=Node("Reserved legacy notice",hud);
   trialSlot=Node("Reserved legacy trial",hud);Place(trialSlot,new Vector2(0,1),new Vector2(180,-326),new Vector2(340,112));
   Text("Desktop controls",hud,"WASD  Move   SPACE  Double jump   SHIFT  Dash   G  Grapple   J  Blade   K  Power   L  Parry   F  Spell   Q  Element",13,new Vector2(.5f,0),new Vector2(0,21),new Vector2(950,28)).gameObject.name="Desktop controls";
  }
  Image Bar(Transform parent,string name,Vector2 position,Color tint){
   var back=Art(name+" border",parent,"button",new Vector2(0,.5f),position,new Vector2(210,26),true);
   var fill=Art(name+" fill",back.transform,"button-active",new Vector2(.5f,.5f),Vector2.zero,new Vector2(198,18));fill.color=tint;fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;return fill;
  }
  void Update(){if(ready){RefreshLayout();Refresh();}}
  // RealmGame explicitly calls this BEFORE reading any finger this frame.
  // This is independent of this component's late text-refresh execution order.
  public void PrepareInputLayout(){if(ready&&isActiveAndEnabled)RefreshLayout();}
  public Rect LegacyNoticeRect=>VirtualRect(noticeSlot);
  public Rect LegacyTrialRect=>VirtualRect(trialSlot);
  // Exclude only native HUD buttons on touch-begin. Existing owned fingers
  // continue through the router, even when dragged across these buttons.
  public bool BlocksGameplayTouch(Vector2 screenPosition){
   return CoversHud&&(RectTransformUtility.RectangleContainsScreenPoint(pauseHit,screenPosition,null)||RectTransformUtility.RectangleContainsScreenPoint(elementHit,screenPosition,null));
  }
  void RefreshLayout(){
   bool show=Application.isMobilePlatform||ShowMobileControlsForCapture;
   Rect area=UnityEngine.Screen.safeArea;
   if(area==lastSafe&&lastWidth==UnityEngine.Screen.width&&lastHeight==UnityEngine.Screen.height&&show==lastMobile)return;
   lastSafe=area;lastWidth=UnityEngine.Screen.width;lastHeight=UnityEngine.Screen.height;lastMobile=show;
   safe.anchorMin=new Vector2(area.xMin/lastWidth,area.yMin/lastHeight);safe.anchorMax=new Vector2(area.xMax/lastWidth,area.yMax/lastHeight);
   mobile.gameObject.SetActive(show);hud.Find("Desktop controls").gameObject.SetActive(!show);
   Canvas.ForceUpdateCanvases();
   float noticeWidth=Mathf.Max(240,safe.rect.width-568);
   Place(noticeSlot,new Vector2(0,1),new Vector2(360+noticeWidth*.5f,-218),new Vector2(noticeWidth,88));
   float menuScale=Mathf.Min(1,Mathf.Min((safe.rect.width-12)/560,(safe.rect.height-12)/674));
   menu.localScale=Vector3.one*menuScale;
   pause.localScale=Vector3.one*Mathf.Min(1,Mathf.Min((safe.rect.width-12)/560,(safe.rect.height-12)/574));
   if(show){for(int i=0;i<6;i++)actionRects[i]=VirtualRect(actions[i]);TouchRouter.SetLayout(actionRects,VirtualRect(moveRegion),VirtualRect(cameraRegion));}
   else TouchRouter.ClearLayout();
  }
  Rect VirtualRect(RectTransform rect){
   rect.GetWorldCorners(corners);var min=RectTransformUtility.WorldToScreenPoint(null,corners[0]);var max=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
   return new Rect(min.x*1280/lastWidth,(lastHeight-max.y)*720/lastHeight,(max.x-min.x)*1280/lastWidth,(max.y-min.y)*720/lastHeight);
  }
  void Refresh(){
   menu.gameObject.SetActive(game.Screen==GameScreen.Menu&&!game.ArsenalVisible);
   pause.gameObject.SetActive(game.Screen==GameScreen.Paused&&!game.ArsenalVisible);
   hud.gameObject.SetActive(game.Screen==GameScreen.Playing&&game.Player);
   if(game.Screen!=GameScreen.Menu)DismissConfirmation();
   if(menu.gameObject.activeSelf){
    int total=0;foreach(var value in game.Save.stars)total+=value;
    Set(journey,game.Save.unlocked+" / 15");Set(stars,total+" / 45");Set(gold,game.Save.coins.ToString());Set(gems,game.Save.gems.ToString());
    Set(location,RealmGame.Titles[Mathf.Clamp(game.Save.unlocked-1,0,14)]);
   }
   if(pause.gameObject.activeSelf)Set(pauseInfo,RealmGame.Titles[game.Level-1]+"  •  "+RealmGame.Clock(game.Elapsed));
   if(!hud.gameObject.activeSelf)return;
   var player=game.Player;
   Set(chapter,"CH "+game.Level.ToString("00"));Set(weapon,game.CurrentWeapon.Name);
   Set(hpText,"HP  "+player.Health+" / "+player.MaxHealth);Set(energyText,"AETHER  "+Mathf.RoundToInt(player.Energy)+" / 100");
   hpFill.fillAmount=Mathf.Clamp01((float)player.Health/player.MaxHealth);energyFill.fillAmount=Mathf.Clamp01(player.Energy/100f);
   Set(objectives,"GOLD "+game.Coins+"/"+game.CoinsTotal+"   GEMS "+game.Gems+"/"+game.GemsTotal+"\nFOES "+game.Kills+"/"+game.KillsTotal+"   •   "+(game.GateOpen()?"GATE OPEN":((int)(game.Completion()*100))+"% / 60% TO OPEN"));
   Set(timer,RealmGame.Clock(game.Elapsed));Set(combat,player.CounterReady?"COUNTER READY • STRIKE NOW":game.Combo>=3?"COMBO ×"+game.Combo:"");
   Set(element,(player.Power==0?"EMBER":player.Power==1?"FROST":"GALE")+"  •  CHANGE ELEMENT");element.color=RealmGame.ElementColors[player.Power];
   handle.anchoredPosition=game.TouchMove*48;
   if(!portraitAttempted&&player.Visual){
    portraitAttempted=true;
    try{heroPortrait=FantasyPortrait.Capture(player);portrait.texture=heroPortrait;portrait.enabled=true;portraitFallback.gameObject.SetActive(false);}
    catch(Exception error){Debug.LogError("FantasyUI: genuine Aster portrait unavailable; retaining ASTER label, not substitute artwork. "+error);}
   }
   if(game.World){
    Vector3 pos=player.transform.position,target=game.World.Route.Count>0?game.World.Route[game.World.Route.Count-1]+new Vector3(0,0,5):new Vector3(0,pos.y,game.World.EndZ);float best=float.MaxValue;bool found=false;
    foreach(var node in game.World.Route){float dz=node.z-pos.z;if(dz>1&&dz<best){best=dz;target=node;found=true;}}
    Vector3 to=target-pos;to.y=0;float angle=Vector3.SignedAngle(player.transform.forward,to,Vector3.up);
    Set(compass,(angle < -20?"←":angle>20?"→":"↑")+" "+Mathf.RoundToInt(to.magnitude)+"m\n"+(found?"NEXT ISLE":"REALM GATE"));
   }
  }
  void OnEnable(){lastWidth=0;if(safe)safe.gameObject.SetActive(true);}
  void OnDisable(){TouchRouter.ClearLayout();if(safe)safe.gameObject.SetActive(false);}
  void OnDestroy(){TouchRouter.ClearLayout();if(heroPortrait){heroPortrait.Release();Destroy(heroPortrait);}if(runtimeHeading)Destroy(runtimeHeading);if(runtimeBody)Destroy(runtimeBody);}
 }
}
