# Clouds
  - "'No popcorn' clouds"
  - "Alternate lighting for cloud groups"
  - "Cirrus lighting"
  - 
  - "Reduce cloud brightness at dawn/dusk/night"
  - "Reduce top layer cloud brightness at dawn/dusk/night"
  - "Cloud shadow extended size"




// Rayleigh Scattering for V5, insert above "foggedColor.rgb += fogBlend[0] * (first.cb_mFogColor.rgb + ambient);"
  if ((cb_mObjectType != (uint)1) && (cb_mObjectType != (uint)21) && (cb_mObjectType != (uint)19))
  {
    const float DensFactor = 0.0000000590;
    const float DistK = 5.38 * (1 - saturate(exp(-distanceSq * DensFactor))) * saturate(cb_mSun.mDiffuse.g - 0.15);
   foggedColor.rgb = foggedColor.rgb * (1 - float3(0.00, 0.028, 0.148) * DistK) + float3(0.00, 0.028, 0.148) * DistK;
  }
