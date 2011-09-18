struct VertexToPixel
{
    float4 Position     : POSITION;
    float4 Color        : COLOR0;
};

struct PixelToFrame
{
    float4 Color : COLOR0;
};

float4x4 ViewProjection;
//float AspectRatioSquared; // ypixels / x pixels
float XPixels;
float YPixels;
float LineWidth; // ypixels / x pixels

VertexToPixel LineVertexShader( float4 startPos : POSITION, float4 endPos : TEXCOORD0, float2 textPos : TEXCOORD1, float4 inColor : COLOR0)
{
    VertexToPixel Output = (VertexToPixel)0;
    float4 start3D = mul(startPos, ViewProjection);
	float4 end3D = mul(endPos, ViewProjection);
	float4 position;
	start3D = start3D / start3D.w;
	end3D = end3D / end3D.w;
	//float2 line2DNormal = float2(-(start3D.y - end3D.y) * AspectRatioSquared, (start3D.x - end3D.x));
	float2 line2DNormal = float2(-(start3D.y - end3D.y) * YPixels, (start3D.x - end3D.x) * XPixels);
	line2DNormal = normalize(line2DNormal); // This is normal in pixel space
	line2DNormal.x = line2DNormal.x * 2 * LineWidth / XPixels;
	line2DNormal.y = line2DNormal.y * 2 * LineWidth / YPixels;
	if (textPos.x == 1.0)
		position = end3D;
	else
		position = start3D;
	position.x = position.x + textPos.y * line2DNormal.x;
	position.y = position.y + textPos.y * line2DNormal.y;

	Output.Position = position;
    Output.Color.rgb = inColor.yxz;
    return Output;    
}


 PixelToFrame LinePixelShader(VertexToPixel PSIn)
 {
     PixelToFrame Output = (PixelToFrame)0;
 
	 Output.Color.rgb = PSIn.Color.yxz;
	 //Output.Color.a = PSIn.Color.a;
	 Output.Color.a = 1.0f;
 
     return Output;
 }


technique Simplest
{
    pass Pass0
    {        
        VertexShader = compile vs_2_0 LineVertexShader();

        PixelShader = compile ps_2_0 LinePixelShader();

    }
}