uint StencilRead_float(float4 c : COORD, out uint stencilOut) : SV_StencilRef
{
    return stencilOut = uint(c.x);
}