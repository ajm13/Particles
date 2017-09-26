float binWidth;
uint gridWidth, numBins;

RWStructuredBuffer<int> temp;
RWStructuredBuffer<int> binCounts;
RWStructuredBuffer<int> binIds;
RWStructuredBuffer<Particle> nnsBins;

[numthreads(256, 1, 1)]
void ZeroBuffers(uint3 id : SV_DispatchThreadID)
{
    binCounts[id.x] = 0;
    temp[id.x] = 0;
}

[numthreads(256, 1, 1)]
void CountBins(uint3 id : SV_DispatchThreadID)
{
    uint i = id.x, bid;

    if (i >= numParticles)
        return;

    int3 pos = (particles[i].pos + (float3) radius) / binWidth;
    uint bin = pos.x + gridWidth * (pos.y + gridWidth * pos.z);
    
    InterlockedAdd(binCounts[bin], 1, bid);
    
    particles[i].bin = bin;
    particles[i].id = bid;
}

uint o;

[numthreads(256, 1, 1)]
void CopyCounts(uint id : SV_DispatchThreadID)
{
    temp[id.x] = (id.x > 0) ? binCounts[id.x - 1] : 0;
}

[numthreads(256, 1, 1)]
void ScanIter(uint id : SV_DispatchThreadID)
{
    binIds[id.x] = temp[id.x] + ((id.x >= o) ? temp[id.x - o] : 0);
}

[numthreads(256, 1, 1)]
void SwapBuffers(uint id : SV_DispatchThreadID)
{
    temp[id.x] = binIds[id.x];
}

[numthreads(256, 1, 1)]
void SortBins(uint3 id : SV_DispatchThreadID)
{
    uint i = id.x;
    if (i >= numParticles)
        return;

    int bid = binIds[particles[i].bin];
    nnsBins[bid + particles[i].id] = particles[i];
}

void GetNNBins(uint bin, out int bins[27], float3 pos, float r)
{
    uint3 bin3d = (uint3) 0;
    bin3d.x = bin % gridWidth;
    bin3d.y = (bin / gridWidth) % gridWidth;
    bin3d.z = (bin / (gridWidth * gridWidth)) % gridWidth;

    //float3 binPos = -radius + binWidth * (0.5 + (float3) bin3d);
    
    for (uint i = 0; i < 27; i++)
        bins[i] = -1;

    for (uint x = 0; x < 3; x++) {
        if (x == 0 && bin3d.x == 0) //|| pos.x - r > binPos.x - 0.5 * binWidth))
            continue;
        if (x == 2 && bin3d.x == gridWidth - 1) //|| pos.x + r < binPos.x + 0.5 * binWidth))
            continue;

        for (uint y = 0; y < 3; y++) {
            if (y == 0 && bin3d.y == 0) //|| pos.y - r > binPos.y - 0.5 * binWidth))
                continue;
            if (y == 2 && bin3d.y == gridWidth - 1) //|| pos.y + r < binPos.y + 0.5 * binWidth))
                continue;

            for (uint z = 0; z < 3; z++) {
                if (z == 0 && bin3d.z == 0) //|| pos.z - r < binPos.z - 0.5 * binWidth))
                    continue;
                if (z == 2 && bin3d.z == gridWidth - 1) //|| pos.z + r < binPos.z + 0.5 * binWidth))
                    continue;

                bins[x + 3 * (y + 3 * z)] = bin + (x - 1)
                    + gridWidth * ((y - 1) + gridWidth * (z - 1));
            }
        }
    }
}
