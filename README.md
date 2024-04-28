# CDEP Unity Implementation
this is a VR implementation of CDEP. The repo has been tested on an HTC Vive and should work well on other headset though is untested. If a headset is present then it should automatically connect to unity and start tracking on play. Additionally, the project makes use of the MockHMD XR Plugin allowing for development without a headset plugged in. Camera movement is done manually via the camera transform in the inspector in this state. 
## Project Structure 
Currently data is loaded in at runtime from the special [streaming assets folder](https://docs.unity3d.com/Manual/StreamingAssets.html). The streaming assets folder should contain a captures.json file such as [this](https://github.com/ust-vis/CDEPVR/files/15140504/captures.json) that contains relative paths to color and depth files as well as positions. Color Images have been tested to work with .png, depth is stored in .depth files which are just a series of floats. This data is not provided in the repo and will need to be added separately. There are 3 scenes scenes setup under Assets/Scenes
### PointCloudScene
This is mostly unused test scene that just loads CDEP data in as a raw point cloud without any re-projection or point sizing.
### BasicSceneCDEP
This scene implements CDEP within the traditional vertex-fragment shader pipeline. It generates two projections of the points into 3D space and uses an orthographic cameras rendering to render textures that are then mapped to spheres that track separate cameras for each eye. Render layers are used to ensure each eye only renders what it is supposed to render. In order to size points in this implementation the HLSL PSIZE semantic was used. This semantic is identical to the gl_pointsize in openGL implementations. This semantic does not work correctly when using the Direct-X unity backend. Instead openGL or Vulkan are required to enable this scene to work correctly. 

This scene has experimental depth support for mixed reality experiences. However, currently the depth is passed linearly causing very severe stair stepping artifacts.

Its also work noting that the code for this implementation is very experimental and messy. Most of this code was re-written for the computer shader implementation in a much cleaner manner. 
### ComputeShaderCDEP
This is the main scene that most of the effort was put into. This is because of the immense performance uplift this implementation has over the traditional pipeline approach. This additional performance overhead allows for the addition of a rudimentary interpolation of captures. In this version a single compute shader outputs one texture. I believe the top half is the left eye and the right eye is the bottom half. Then, this texture is projected onto two spheres scaled vertically by a factor of 2 with different offsets for each eye. Again render layers are used for each eye and the spheres track the eye positions.

The main script for this implementation is the CDEPShaderDispatch script. Data is loaded in as a list of Capture objects via the Resources Class. 

### 

## Development Environment Setup
### C#
Just Visual Studio
### Vertex / Fragment Shaders
Unity wraps shader code in custom syntax known as shader lab. This allows for bindings to the editor as well as metadata. Unfortunatly shaderlab doesn't have any great tooling support. The best I've found is [this VSCode extension](https://marketplace.visualstudio.com/items?itemName=amlovey.shaderlabvscodefree). There is a paid version available that adds live error detection and intellisense but I've made do with the free version. 
### Computer Shaders
These are just standard HLSL files and I've had luck with this [HLSL tools extension](https://marketplace.visualstudio.com/items?itemName=TimGJones.hlsltools)

## Issues
Known Issues are posted under the issues section of the repo
