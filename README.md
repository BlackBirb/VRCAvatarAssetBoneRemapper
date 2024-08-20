# Just use [VRCFury](https://vrcfury.com/components/armature-link)
I've done this before VRCFury had armature link, this is deprecated now.

# ~~VRCAvatarAssetBoneRemapper~~
*Really rolls off the tongue, doesn't it?*

An Unity editor tool that allows to attach assets to a model with matching or simmilar armature (eg. clothing) without the use of bone constraints. 
The tool simply changes the underlying mesh reference to it's armature, it does not weight paint or modify the meshes in any way. It cannot add new bones to your asset.

While this tool was made with VRChat in mind, it still can be used to map any mesh to any armature. 

## Installation
To install the tool, import `.unitypackage` from [releases](https://github.com/BlackBirb/VRCAvatarAssetBoneRemapper/releases) or add the `.cs` file to your project. The tool should appear in `Tools/BlackBird/VRCAvatarASsetBoneRemapper`.

## How to use
1. Drag your model's main (usually a body) mesh renderer to the `Body mesh` slot. ![image](https://user-images.githubusercontent.com/20701563/222249622-e0cc6b6b-3a89-4442-8c14-798942713260.png)
2. Drag your asset mesh rendered to the `Asset mesh` slot.![image](https://user-images.githubusercontent.com/20701563/222250431-4a720154-b463-425b-b4ec-5f483244ae68.png)
3. If your model is humanoid, the `Root bone` should fill automatically, if not simply drag the transform from skinned mesh renderer `Root bone` of your target model.
4. The bones will map automatically but make sure all are mapped correctly, validation can help notice which are missing or missconfigured. Name on the left is coresponding bone from the asset, on the right it's your target model bone. ![image](https://user-images.githubusercontent.com/20701563/222251366-c36cffe5-e456-4468-a587-4b3a0a74877e.png)
5. If there are no missing bones in `Bones to transfer` section skip this point. Select all or some of the bones and press `Transfer missing bones`. This part really needs your judgement which bones should be transfered over.![image](https://user-images.githubusercontent.com/20701563/222251819-ca1244eb-1a24-489b-8d86-b18817ca23ce.png)

6. Press apply
7. Check if your clothing follows armature by twisting one of the bones. *don't forget to undo your twists*
8. If you have multiple meshes, return to point 2.
9. If everything is working you can safely remove the asset prefab from the scene (unpacked and missing the meshes at this point).
 
## Usage notes
I tried to make the tool as simple as possible but it still requires some knowledge about unity and models.
- I only tested the tool on humanoid models. Should work on others, as it's a simple remap, but no promises.
- You need at least one bone that is used by both your target model mesh and in your asset mesh, there has to be something to map!
- If your asset has multiple meshes, add each of them separetly.
- In case your asset has more bones than target model you can try adding the bones using the tool. If this fails, you can try manually move the bone's GameObjects across to your avatar. In that case remap missing bones to themselves when using the tool... otherwise it'll assign it to null and that's a big problem!
- The tool will try to match bones automatically, but it's simply using bone names and easly fails. Always check if everything is set up correctly!
- The tool is non-destructive **only for the target model**. If something goes wrong beyond repair, remove any of the asset meshes and bones created in your armature (if there are any). Your model will be back to normal. *still wouldn't hurt ot make a backup, always make backups!!*
- Object culling is based on root bone as far as I'm aware, this can cause the object to dissapear while still in view if root bone is too far from the object. This can be fixed in skinned mesh rendered objects in unity itself.

## Contact
If you need help, found a bug, want to give feedback or have an idea, use either [Issues](https://github.com/BlackBirb/VRCAvatarAssetBoneRemapper/issues) or message me directly on discord @ BlackBird#9999

### Credits
Big shoutout to [CascadianWorks](https://github.com/CascadianWorks/Unity-Mesh-Transfer-Utility) as it's basically the same idea just with more UI.
