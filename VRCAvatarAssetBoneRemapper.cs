using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Configuration;

#if UNITY_EDITOR
public class VRCAvatarAssetBoneRemapper : EditorWindow
{
    static public string MissingAssetBoneKey = "Unknown";

    private SkinnedMeshRenderer TargetAvatar;
    private Transform AvatarRootBone;

    private SkinnedMeshRenderer Asset;

    private bool ArmatureTreeOpen = true;
    private Vector2 ArmatureMapScroll = new Vector2(0, 0);

    private bool MissingTreeOpen = true;
    private Vector2 MissingMapScroll = new Vector2(0, 0);
    private List<string> MissingFilter = new List<string>();

    private Transform[] newAssetArmature;

    public Dictionary<string, Transform> TargetArmatureLookup;

    public bool validate = true;

    [MenuItem("Tools/BlackBird/AssetBoneRemappers")]
    
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        VRCAvatarAssetBoneRemapper window = (VRCAvatarAssetBoneRemapper)EditorWindow.GetWindow(typeof(VRCAvatarAssetBoneRemapper));
        window.minSize = new Vector2(425, 600);
        window.Show();
    }

    public void OnGUI()
    {
        GUILayout.Space(10f);
        GUILayout.Label("Map assets armature onto avatar armature", EditorStyles.boldLabel);

        // Target avatar input
        GUILayout.Space(10f);
        GUILayout.Label("Body Mesh of your target avatar:", EditorStyles.boldLabel);
        this.TargetAvatar = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(this.TargetAvatar, typeof(SkinnedMeshRenderer), true);

        if(this.TargetAvatar) {
            if(TargetArmatureLookup == null) {
                this.loadTargetArature();
            }

            // Asset Mesh input
            GUILayout.Space(10f);
            GUILayout.Label("Asset mesh:", EditorStyles.boldLabel);
            GUILayout.Label("Run the script multiple times if there are multiple meshes in an object.", EditorStyles.helpBox);
            Asset = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(Asset, typeof(SkinnedMeshRenderer), true);
            
            // Root bone input, should read itself when loading armature
            GUILayout.Space(10f);
            GUILayout.Label("Avatar root bone:", EditorStyles.boldLabel);
            GUILayout.Label("Usually the humanoid hip from animator.", EditorStyles.helpBox);
            AvatarRootBone = (Transform)EditorGUILayout.ObjectField("Root bone", AvatarRootBone, typeof(Transform), true);

        } else {
            // cllean up 
            TargetArmatureLookup = null;
            AvatarRootBone = null;
            // when asset goes null, all it's responsibilites will be gone too >:2
            Asset = null;
        }

        if(Asset != null) {
            // QOL: try matching bones by names initially
            if(newAssetArmature == null)
                this.tryAutofillByName();

            EditorGUILayout.Space(10f);
            
            // Armature match seciton
            GUILayout.Space(10f);
            GUILayout.Label("Armature match:", EditorStyles.boldLabel);

            if (GUILayout.Button("Match bones by name")) {
                this.tryAutofillByName();
            }

            // Validation toggle
            validate = EditorGUILayout.BeginToggleGroup("Validate", validate);
            EditorGUILayout.EndToggleGroup();

            // Armature collapse
            ArmatureTreeOpen = EditorGUILayout.BeginFoldoutHeaderGroup(ArmatureTreeOpen, "Armature");
            if(ArmatureTreeOpen) {
                ArmatureMapScroll = EditorGUILayout.BeginScrollView(ArmatureMapScroll);
                for(int i = 0; i < Asset.bones.Length; i++) {
                    string boneKey;
                    if(Asset.bones[i] != null) {
                        boneKey = Asset.bones[i].gameObject.name;
                    } else {
                        // This happens if user applies asset without copying bones
                        // Very bad as it's impossible to recover from, bone position data is lost
                        boneKey = VRCAvatarAssetBoneRemapper.MissingAssetBoneKey;
                    }

                    // Get validation message
                    if(validate){
                        string message = "";
                        if(newAssetArmature[i] == null) {
                            message += "Missing target bone! ";
                        } else {
                            if(boneKey != VRCAvatarAssetBoneRemapper.MissingAssetBoneKey && newAssetArmature[i].gameObject.name != boneKey)
                                message += "Name missmatch! ";
                        }
                        if(message.Length > 0) {
                            EditorGUILayout.Space(4f);
                            EditorGUILayout.LabelField(message, EditorStyles.centeredGreyMiniLabel);
                        }
                    }
                    
                    newAssetArmature[i] = (Transform)EditorGUILayout.ObjectField(boneKey, newAssetArmature[i] ?? null, typeof(Transform), true);
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Tool for fixing missing bones
            // I should start separating all this to functions, shouldn't I?
            GUILayout.Space(10f);
            GUILayout.Label("Missing bones:", EditorStyles.boldLabel);
            GUILayout.Label("Attempt copying missing bones from the asset over to the target avatar. This tool assumes at least one bone is matched across.\nThis won't work after the asset has been applied.", EditorStyles.helpBox);

            if (GUILayout.Button("Transfer missing bones")) {
                this.transferMissingBones();
            }

            MissingTreeOpen = EditorGUILayout.BeginFoldoutHeaderGroup(MissingTreeOpen, "Bones to transfer");
            if(MissingTreeOpen) {
                MissingMapScroll = EditorGUILayout.BeginScrollView(MissingMapScroll);

                // Select all "button"
                // It being a checkbox is for UX (?)
                EditorGUILayout.BeginHorizontal();
                bool selectAll = EditorGUILayout.Toggle(false, GUILayout.MaxWidth(20f));
                GUILayout.Label("Select all", EditorStyles.label);
                EditorGUILayout.EndHorizontal();

                for(int i = 0; i < Asset.bones.Length; i++) {
                    Transform assetBone = Asset.bones[i];
                    // We only care about the non-mapped ones
                    if(this.newAssetArmature[i] != null) continue;

                    // Very confusing way to filter using lists
                    // don't ask me why i did it this way, idk C#
                    EditorGUILayout.BeginHorizontal();
                    int filterIndex = MissingFilter.IndexOf(assetBone.gameObject.name);
                    bool shouldAdd = EditorGUILayout.Toggle(selectAll || filterIndex > -1, GUILayout.MaxWidth(20f));
                    if(shouldAdd)
                        MissingFilter.Add(assetBone.gameObject.name);
                    else if(filterIndex > -1)
                        MissingFilter.RemoveAll(str => str == assetBone.gameObject.name); // no idea why but other ways of removing it didn't work ??

                    EditorGUILayout.ObjectField(assetBone, typeof(Transform), true);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();


        } else {
            // clear up stuff when asset is gone
            newAssetArmature = null;
            MissingFilter = new List<string>();
            ArmatureTreeOpen = false;
            MissingTreeOpen = true;
            validate = true;
        }

        // A magic trick~
        EditorGUILayout.BeginScrollView(Vector2.zero);
        EditorGUILayout.EndScrollView();
        // FR tho idk how to move apply button to the bottom and I want it on the bottom so here we are

        GUILayout.Space(10f);
        if (GUILayout.Button("Apply"))
        {
            if(this.transferArmatures()) {
                this.mergeObjects();
                Asset = null;
            }
        }
        
    }
    
    private void transferMissingBones() {
        bool finished = false;
        int safeguard = 0;

        Dictionary<string, Transform> currentArmature = new Dictionary<string, Transform>();
        // i'd rather start off with mapped bones but it doesn't want to work, idk why
        foreach(Transform bone in this.TargetAvatar.bones) {
            currentArmature[bone.gameObject.name] = bone;
        }

        // most probably will run once, but in case some weird parenting happens, this will ensure all bones are moved over
        while(!finished) {
            finished = true;

            for(int i = 0; i < Asset.bones.Length; i++) {
                Transform assetBone = Asset.bones[i];
                // ignore deselected bones
                if(MissingFilter.IndexOf(assetBone.gameObject.name) >= 0) continue;

                if(this.newAssetArmature[i] != null) continue;

                // move bone over only if we got target parent.. kinda duh
                Transform parentTarget = null;
                if(!currentArmature.TryGetValue(assetBone.parent.gameObject.name, out parentTarget)) continue;

                finished = false;
                GameObject newBone = new GameObject(assetBone.gameObject.name);
                newBone.transform.SetParent(parentTarget);
                newBone.transform.localPosition = assetBone.localPosition;
                newBone.transform.localScale = assetBone.localScale;
                newBone.transform.localRotation = assetBone.localRotation;

                // put new bone into lookup so all children get copied one by one
                // why not copy children along then?
                // it's in case only one bone from entire tree is missing but.. it'lll blow up anyways so I'm not sure now
                currentArmature[assetBone.gameObject.name] = newBone.transform;
                this.newAssetArmature[i] = newBone.transform;
            }


            // we don't want infinite loops and if your armature is messed up enough to do it 10 times then this tool won't help you anyways. 
            // Also it'll mess you even more .c.
            if(safeguard++ >= 10) break;
        }
    }

    // This is where the magic happens✨
    private bool transferArmatures() {
        if(this.TargetAvatar == null) {
            // how to show errors?
            return false;
        }
        if(Asset == null) {
            // HOW DO I SHOW ERRORS!? lol
            return false;
        }

        // I'm just not sure how C# handles arrays so I just manually copy it
        Transform[] tempArmature = new Transform[Asset.bones.Length];
        for(int i = 0; i < Asset.bones.Length; i++) {
            // TODO:
            // Waht happens if not all bones are mapped? Currently set to null to preserve bone count and order
            // but I'm not sure how unity will handle this
            tempArmature[i] = newAssetArmature[i];
        }
        Asset.bones = tempArmature;
        return true;
    }

    // Move mesh gameObject into avatar gameObject and set the parent bone
    private void mergeObjects() {
        if(AvatarRootBone == null)
            return;

        // unpack the asset if needed
        if (PrefabUtility.GetPrefabInstanceStatus(Asset.transform.parent.gameObject) == PrefabInstanceStatus.Connected)
            PrefabUtility.UnpackPrefabInstance(Asset.transform.parent.gameObject, unpackMode: PrefabUnpackMode.Completely, action: InteractionMode.AutomatedAction);

        Asset.gameObject.transform.SetParent(this.TargetAvatar.gameObject.transform.parent);
        // I swear if this is not set by the time you clicked the button I'll smack you
        // Idk how to undo everthing at this point just so you can add it back, enjoy the consequences
        Asset.rootBone = AvatarRootBone;
    }

    // Load our target avatar armature into dictionary for easy lookup when autofilling
    private void loadTargetArature() {
        if(this.TargetAvatar != null) {
            AvatarRootBone = this.TargetAvatar.rootBone;
            TargetArmatureLookup = new Dictionary<string, Transform>();
            foreach(Transform bone in this.TargetAvatar.bones) {
                TargetArmatureLookup[bone.gameObject.name] = bone;
            }
        }
    }

    // QOL function to try and match bones by ther name
    private void tryAutofillByName() {
        if(Asset != null) {
            newAssetArmature = new Transform[Asset.bones.Length];
            for(int i = 0; i < Asset.bones.Length; i++) {
                // In case asset doesn't have bones itself.. how bad at blender are you if this happens??
                if(Asset.bones[i] == null) continue;
                GameObject bone = Asset.bones[i].gameObject;
                TargetArmatureLookup.TryGetValue(bone.name, out newAssetArmature[i]);
            }
        }
    }
}
#endif