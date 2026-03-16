
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class RebuildScene
{
    static readonly Color BG = H("2A1215"); static readonly Color FELT = H("1B6B35"); static readonly Color FELT_L = H("237A42");
    static readonly Color RAIL_O = H("1A0A05"); static readonly Color RAIL_I = H("5C3A1E"); static readonly Color PANEL = H("1A0A08",0.95f);
    static readonly Color GOLD = H("FFD700"); static readonly Color G_BTN = H("43A047"); static readonly Color B_BTN = H("1E88E5");
    static readonly Color GR_BTN = H("4A3A3A"); static readonly Color ORANGE = H("FFB347"); static readonly Color TW = H("F0F0F0");
    static readonly Color TD = H("AAAAAA"); static readonly Color SBG = new Color(0,0,0,0.5f); static readonly Color CE = new Color(1,1,1,0.05f);

    [MenuItem("Tools/Rebuild Scene")]
    public static void Build()
    {
        var oval = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/OvalFelt.png");
        if(!oval){Debug.LogError("OvalFelt.png missing!");return;}
        var pfb = HPfb();
        var sc = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Single);
        Camera.main.backgroundColor=BG;Camera.main.clearFlags=CameraClearFlags.SolidColor;
        var cv=new GameObject("PokerTableCanvas");var c=cv.AddComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;
        var s=cv.AddComponent<CanvasScaler>();s.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution=new Vector2(1920,1080);s.matchWidthOrHeight=0.5f;cv.AddComponent<GraphicRaycaster>();
        Vector2 TC=new Vector2(-80,15);
        Oval("RO",cv.transform,oval,RAIL_O,TC,1220,710);Oval("RI",cv.transform,oval,RAIL_I,TC,1180,675);
        var felt=Oval("TableFelt",cv.transform,oval,FELT,TC,1120,630);Oval("FH",cv.transform,oval,FELT_L,TC,920,490);
        // CC
        var cca=U("CommunityCardsArea",cv.transform);R(cca,0.5f,0.5f,0.5f,0.5f,TC+new Vector2(0,30),new Vector2(440,100));
        var h1=cca.AddComponent<HorizontalLayoutGroup>();h1.spacing=8;h1.childAlignment=TextAnchor.MiddleCenter;
        h1.childForceExpandWidth=false;h1.childForceExpandHeight=false;
        var ccv=new HijackPoker.UI.CardView[5];
        for(int i=0;i<5;i++)ccv[i]=Crd($"CC{i+1}",cca.transform,70,97);
        var ccc=cca.AddComponent<HijackPoker.UI.CommunityCardsView>();WA(ccc,"_slots",ccv);
        var pt=U("PotText",cv.transform);var ptT=pt.AddComponent<TextMeshProUGUI>();
        ptT.fontSize=22;ptT.fontStyle=FontStyles.Bold;ptT.alignment=TextAlignmentOptions.Center;ptT.color=GOLD;
        R(pt,0.5f,0.5f,0.5f,0.5f,TC+new Vector2(0,-35),new Vector2(300,35));
        // Seats
        Vector2[] sp={TC+new Vector2(-260,340),TC+new Vector2(260,340),TC+new Vector2(520,0),
            TC+new Vector2(260,-310),TC+new Vector2(-260,-310),TC+new Vector2(-520,0)};
        var svs=new HijackPoker.UI.SeatView[6];
        for(int i=0;i<6;i++)svs[i]=Seat(i+1,cv.transform,sp[i]);
        var tv=felt.AddComponent<HijackPoker.UI.TableView>();
        // HUD
        var hud=U("HUDPanel",cv.transform);hud.AddComponent<Image>().color=PANEL;
        var hR=hud.GetComponent<RectTransform>();hR.anchorMin=new Vector2(0,1);hR.anchorMax=new Vector2(1,1);
        hR.pivot=new Vector2(0.5f,1);hR.anchoredPosition=Vector2.zero;hR.sizeDelta=new Vector2(0,52);
        var hn=U("HandNumberText",hud.transform);var hnT=hn.AddComponent<TextMeshProUGUI>();
        hnT.text="HIJACK POKER";hnT.fontSize=17;hnT.fontStyle=FontStyles.Bold;hnT.alignment=TextAlignmentOptions.Left;
        hnT.color=GOLD;hnT.characterSpacing=3;A(hn,0,0,0.2f,1,new Vector2(20,0),Vector2.zero);
        var ph=U("PhaseLabel",hud.transform);var phT=ph.AddComponent<TextMeshProUGUI>();
        phT.text="Connecting...";phT.fontSize=15;phT.alignment=TextAlignmentOptions.Center;phT.color=ORANGE;
        A(ph,0.2f,0,0.55f,1,Vector2.zero,Vector2.zero);
        var pd=U("PotDisplayText",hud.transform);var pdT=pd.AddComponent<TextMeshProUGUI>();
        pdT.fontSize=16;pdT.fontStyle=FontStyles.Bold;pdT.alignment=TextAlignmentOptions.Center;pdT.color=GOLD;
        A(pd,0.55f,0,0.75f,1,Vector2.zero,Vector2.zero);
        var ac=U("ActionText",hud.transform);var acT=ac.AddComponent<TextMeshProUGUI>();
        acT.fontSize=13;acT.alignment=TextAlignmentOptions.Right;acT.color=TD;
        A(ac,0.75f,0,1,1,Vector2.zero,new Vector2(-16,0));
        var hv=hud.AddComponent<HijackPoker.UI.HudView>();
        // Ctrl
        var ct=U("ControlsPanel",cv.transform);ct.AddComponent<Image>().color=PANEL;
        var cR=ct.GetComponent<RectTransform>();cR.anchorMin=new Vector2(0,0);cR.anchorMax=new Vector2(1,0);
        cR.pivot=new Vector2(0.5f,0);cR.anchoredPosition=Vector2.zero;cR.sizeDelta=new Vector2(0,56);
        var ch=ct.AddComponent<HorizontalLayoutGroup>();ch.spacing=14;ch.childAlignment=TextAnchor.MiddleCenter;
        ch.padding=new RectOffset(20,20,8,8);ch.childForceExpandWidth=false;ch.childForceExpandHeight=false;
        var nb=Btn("NextStepButton","Next Step",ct.transform,140,40,G_BTN,15);
        var ab=Btn("AutoPlayButton","Auto Play",ct.transform,140,40,B_BTN,15);
        var abT=ab.GetComponentInChildren<TextMeshProUGUI>();
        var sg=U("SpeedGroup",ct.transform);var sgH=sg.AddComponent<HorizontalLayoutGroup>();
        sgH.spacing=5;sgH.childAlignment=TextAnchor.MiddleCenter;sgH.childForceExpandWidth=false;sgH.childForceExpandHeight=false;
        sg.AddComponent<LayoutElement>().preferredWidth=250;
        string[]spd={"0.25s","0.5s","1s","2s"};var sB=new Button[4];var sI=new Image[4];
        for(int i=0;i<4;i++){var b=Btn($"S{spd[i]}",spd[i],sg.transform,55,32,GR_BTN,12);sB[i]=b.GetComponent<Button>();sI[i]=b.GetComponent<Image>();}
        var ctV=ct.AddComponent<HijackPoker.UI.ControlsView>();
        // Hist
        var hi=U("HistoryPanel",cv.transform);hi.AddComponent<Image>().color=H("150808",0.95f);
        var hiR=hi.GetComponent<RectTransform>();hiR.anchorMin=new Vector2(1,0);hiR.anchorMax=new Vector2(1,1);
        hiR.pivot=new Vector2(1,0.5f);hiR.anchoredPosition=Vector2.zero;hiR.offsetMin=new Vector2(-240,56);hiR.offsetMax=new Vector2(0,-52);
        var hVL=hi.AddComponent<VerticalLayoutGroup>();hVL.spacing=3;hVL.padding=new RectOffset(8,8,8,8);
        hVL.childForceExpandWidth=true;hVL.childForceExpandHeight=false;
        var hdr=U("Hdr",hi.transform);var hdrT=hdr.AddComponent<TextMeshProUGUI>();hdrT.text="HAND HISTORY";
        hdrT.fontSize=12;hdrT.fontStyle=FontStyles.Bold;hdrT.alignment=TextAlignmentOptions.Center;hdrT.color=TD;
        hdrT.characterSpacing=3;hdr.AddComponent<LayoutElement>().preferredHeight=24;
        var dv=U("Div",hi.transform);dv.AddComponent<Image>().color=new Color(1,1,1,0.08f);dv.AddComponent<LayoutElement>().preferredHeight=1;
        var scr=U("Scr",hi.transform);var sr=scr.AddComponent<ScrollRect>();sr.horizontal=false;sr.vertical=true;sr.scrollSensitivity=20;
        scr.AddComponent<LayoutElement>().flexibleHeight=1;
        var vp=U("VP",scr.transform);vp.AddComponent<Image>().color=Color.clear;vp.AddComponent<Mask>().showMaskGraphic=false;F(vp);
        var cn=U("Cnt",vp.transform);var cnV=cn.AddComponent<VerticalLayoutGroup>();cnV.spacing=2;
        cnV.padding=new RectOffset(2,2,2,2);cnV.childForceExpandWidth=true;cnV.childForceExpandHeight=false;
        cn.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var cnR=cn.GetComponent<RectTransform>();cnR.anchorMin=new Vector2(0,1);cnR.anchorMax=new Vector2(1,1);
        cnR.pivot=new Vector2(0.5f,1);cnR.sizeDelta=Vector2.zero;sr.viewport=vp.GetComponent<RectTransform>();sr.content=cnR;
        var hiV=hi.AddComponent<HijackPoker.UI.HandHistoryView>();
        // Status
        var st=U("StatusBar",cv.transform);var stI=st.AddComponent<Image>();stI.color=H("2A1515",0.9f);
        var stR=st.GetComponent<RectTransform>();stR.anchorMin=new Vector2(0,0);stR.anchorMax=new Vector2(1,0);
        stR.pivot=new Vector2(0.5f,0);stR.anchoredPosition=new Vector2(0,56);stR.sizeDelta=new Vector2(0,22);
        var stx=U("STxt",st.transform);var stT=stx.AddComponent<TextMeshProUGUI>();stT.text="Connecting...";
        stT.fontSize=11;stT.alignment=TextAlignmentOptions.Left;stT.color=TD;A(stx,0,0,1,1,new Vector2(12,0),new Vector2(-12,0));
        var stV=st.AddComponent<HijackPoker.UI.StatusBarView>();
        // GM
        var gm=new GameObject("GameManager");
        var api=gm.AddComponent<HijackPoker.Api.PokerApiClient>();
        var sm=gm.AddComponent<HijackPoker.Managers.TableStateManager>();
        var gmC=gm.AddComponent<HijackPoker.Managers.GameManager>();
        // Wire
        W(gmC,"_apiClient",api);W(gmC,"_stateManager",sm);
        W(tv,"_stateManager",sm);W(tv,"_communityCardsView",ccc);WA(tv,"_seatViews",svs);
        W(hv,"_stateManager",sm);W(hv,"_phaseLabel",phT);W(hv,"_handNumberText",hnT);W(hv,"_actionText",acT);W(hv,"_potText",pdT);
        W(ctV,"_gameManager",gmC);W(ctV,"_nextStepButton",nb.GetComponent<Button>());
        W(ctV,"_autoPlayButton",ab.GetComponent<Button>());W(ctV,"_autoPlayButtonText",abT);
        WA(ctV,"_speedButtons",sB);WA(ctV,"_speedButtonImages",sI);
        W(hiV,"_stateManager",sm);W(hiV,"_content",cn.transform);W(hiV,"_scrollRect",sr);W(hiV,"_entryPrefab",pfb);
        W(stV,"_stateManager",sm);W(stV,"_statusText",stT);W(stV,"_background",stI);
        if(!Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>()){
            var es=new GameObject("EventSystem");es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();}
        EditorSceneManager.SaveScene(sc,"Assets/Scenes/PokerTable.unity");
        Debug.Log("Scene rebuilt!");
    }

    static HijackPoker.UI.SeatView Seat(int n,Transform p,Vector2 pos)
    {
        var root=U($"Seat{n}",p);R(root,0.5f,0.5f,0.5f,0.5f,pos,new Vector2(200,160));
        var cg=root.AddComponent<CanvasGroup>();
        var brd=U("Border",root.transform);var bI=brd.AddComponent<Image>();bI.color=Color.clear;F(brd);
        var bg=U("Bg",root.transform);var bgI=bg.AddComponent<Image>();bgI.color=SBG;
        var bgR=bg.GetComponent<RectTransform>();bgR.anchorMin=Vector2.zero;bgR.anchorMax=Vector2.one;
        bgR.offsetMin=new Vector2(2,2);bgR.offsetMax=new Vector2(-2,-2);
        var nm=U("Name",root.transform);var nmT=nm.AddComponent<TextMeshProUGUI>();
        nmT.fontSize=14;nmT.fontStyle=FontStyles.Bold;nmT.alignment=TextAlignmentOptions.Center;nmT.color=TW;
        A(nm,0.05f,0.82f,0.95f,0.98f,Vector2.zero,Vector2.zero);
        var sk=U("Stack",root.transform);var skT=sk.AddComponent<TextMeshProUGUI>();
        skT.fontSize=12;skT.alignment=TextAlignmentOptions.Center;skT.color=H("88DD88");
        A(sk,0.05f,0.68f,0.95f,0.82f,Vector2.zero,Vector2.zero);
        var ca=U("Cards",root.transform);var caH=ca.AddComponent<HorizontalLayoutGroup>();
        caH.spacing=5;caH.childAlignment=TextAnchor.MiddleCenter;caH.childForceExpandWidth=false;caH.childForceExpandHeight=false;
        A(ca,0.05f,0.18f,0.95f,0.68f,Vector2.zero,Vector2.zero);
        var c1=Crd("C1",ca.transform,50,70);var c2=Crd("C2",ca.transform,50,70);
        var bt=U("Bet",root.transform);var btT=bt.AddComponent<TextMeshProUGUI>();
        btT.fontSize=11;btT.alignment=TextAlignmentOptions.Center;btT.color=GOLD;
        A(bt,0,0.04f,1,0.18f,new Vector2(4,0),new Vector2(-4,0));
        var at=U("Act",root.transform);var atT=at.AddComponent<TextMeshProUGUI>();
        atT.fontSize=10;atT.alignment=TextAlignmentOptions.Center;atT.color=Color.white;
        A(at,0,-0.06f,0.5f,0.06f,Vector2.zero,Vector2.zero);
        var rk=U("Rank",root.transform);var rkT=rk.AddComponent<TextMeshProUGUI>();
        rkT.fontSize=10;rkT.alignment=TextAlignmentOptions.Center;rkT.color=H("4FC3F7");
        A(rk,0.5f,-0.06f,1,0.06f,Vector2.zero,Vector2.zero);
        var wn=U("Win",root.transform);var wnT=wn.AddComponent<TextMeshProUGUI>();
        wnT.fontSize=13;wnT.fontStyle=FontStyles.Bold;wnT.alignment=TextAlignmentOptions.Center;wnT.color=H("66BB6A");
        A(wn,0,-0.18f,1,-0.04f,Vector2.zero,Vector2.zero);
        var db=Bdg("D",root.transform,GOLD);var sb=Bdg("SB",root.transform,H("81D4FA"));
        var bb=Bdg("BB",root.transform,H("CE93D8"));
        db.SetActive(false);sb.SetActive(false);bb.SetActive(false);
        var sv=root.AddComponent<HijackPoker.UI.SeatView>();var so=new SerializedObject(sv);
        so.FindProperty("_nameText").objectReferenceValue=nmT;so.FindProperty("_stackText").objectReferenceValue=skT;
        so.FindProperty("_betText").objectReferenceValue=btT;so.FindProperty("_actionText").objectReferenceValue=atT;
        so.FindProperty("_dealerBadge").objectReferenceValue=db;so.FindProperty("_sbBadge").objectReferenceValue=sb;
        so.FindProperty("_bbBadge").objectReferenceValue=bb;so.FindProperty("_card1").objectReferenceValue=c1;
        so.FindProperty("_card2").objectReferenceValue=c2;so.FindProperty("_backgroundImage").objectReferenceValue=bgI;
        so.FindProperty("_borderImage").objectReferenceValue=bI;so.FindProperty("_canvasGroup").objectReferenceValue=cg;
        so.FindProperty("_handRankText").objectReferenceValue=rkT;so.FindProperty("_winningsText").objectReferenceValue=wnT;
        so.ApplyModifiedProperties();return sv;
    }

    static GameObject Oval(string n,Transform p,Sprite sp,Color c,Vector2 pos,float w,float h)
    {var g=U(n,p);var i=g.AddComponent<Image>();i.sprite=sp;i.color=c;R(g,0.5f,0.5f,0.5f,0.5f,pos,new Vector2(w,h));return g;}
    static HijackPoker.UI.CardView Crd(string n,Transform p,float w,float h)
    {var g=U(n,p);var i=g.AddComponent<Image>();i.color=CE;i.preserveAspect=true;
    var l=g.AddComponent<LayoutElement>();l.preferredWidth=w;l.preferredHeight=h;
    var v=g.AddComponent<HijackPoker.UI.CardView>();W(v,"_cardImage",i);return v;}
    static GameObject Bdg(string t,Transform p,Color c)
    {var g=U(t+"Badge",p);g.AddComponent<Image>().color=new Color(0,0,0,0.7f);
    var r=g.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(0.92f,0.92f);r.sizeDelta=new Vector2(30,20);
    var x=U("T",g.transform);var tmp=x.AddComponent<TextMeshProUGUI>();tmp.text=t;tmp.fontSize=11;
    tmp.fontStyle=FontStyles.Bold;tmp.alignment=TextAlignmentOptions.Center;tmp.color=c;F(x);return g;}
    static GameObject HPfb()
    {if(!System.IO.Directory.Exists("Assets/Resources"))System.IO.Directory.CreateDirectory("Assets/Resources");
    var g=new GameObject("HE",typeof(RectTransform));g.AddComponent<LayoutElement>().minHeight=20;
    var t=new GameObject("T",typeof(RectTransform));t.transform.SetParent(g.transform,false);
    var tmp=t.AddComponent<TextMeshProUGUI>();tmp.fontSize=11;tmp.color=TD;tmp.alignment=TextAlignmentOptions.Left;
    tmp.enableWordWrapping=true;tmp.richText=true;F(t);
    var p=PrefabUtility.SaveAsPrefabAsset(g,"Assets/Resources/HistoryEntryPrefab.prefab");
    Object.DestroyImmediate(g);return p;}
    static Color H(string h,float a=1f){ColorUtility.TryParseHtmlString("#"+h,out var c);c.a=a;return c;}
    static GameObject U(string n,Transform p){var g=new GameObject(n,typeof(RectTransform));g.transform.SetParent(p,false);return g;}
    static void R(GameObject g,float a0,float a1,float a2,float a3,Vector2 p,Vector2 s)
    {var r=g.GetComponent<RectTransform>();r.anchorMin=new Vector2(a0,a1);r.anchorMax=new Vector2(a2,a3);r.anchoredPosition=p;r.sizeDelta=s;}
    static void F(GameObject g){var r=g.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    static void A(GameObject g,float a0,float a1,float a2,float a3,Vector2 m,Vector2 x)
    {var r=g.GetComponent<RectTransform>();r.anchorMin=new Vector2(a0,a1);r.anchorMax=new Vector2(a2,a3);r.offsetMin=m;r.offsetMax=x;}
    static GameObject Btn(string n,string l,Transform p,float w,float h,Color bg,int fs)
    {var b=new GameObject(n,typeof(RectTransform));b.transform.SetParent(p,false);b.AddComponent<Image>().color=bg;b.AddComponent<Button>();
    var le=b.AddComponent<LayoutElement>();le.preferredWidth=w;le.preferredHeight=h;
    var t=new GameObject("T",typeof(RectTransform));t.transform.SetParent(b.transform,false);
    var tmp=t.AddComponent<TextMeshProUGUI>();tmp.text=l;tmp.fontSize=fs;tmp.fontStyle=FontStyles.Bold;
    tmp.alignment=TextAlignmentOptions.Center;tmp.color=Color.white;F(t);return b;}
    static void W(Component c,string p,Object v){var so=new SerializedObject(c);so.FindProperty(p).objectReferenceValue=v;so.ApplyModifiedProperties();}
    static void WA<T>(Component c,string p,T[]v)where T:Object{var so=new SerializedObject(c);var a=so.FindProperty(p);
    a.arraySize=v.Length;for(int i=0;i<v.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=v[i];so.ApplyModifiedProperties();}
}
