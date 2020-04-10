mpVector LinearInterp(mp4Vector p1, mp4Vector p2, float value)
{
    if (p2 < p1)
    {
        mp4Vector temp;
        temp = p1;
        p1 = p2;
        p2 = temp;    
    }

    mpVector p;
    if(fabs(p1.val - p2.val) > 0.00001)
        p = (mpVector)p1 + ((mpVector)p2 - (mpVector)p1)/(p2.val - p1.val)*(value - p1.val);
    else 
        p = (mpVector)p1;
    return p;
}

    bool operator<(const mp4Vector &right) const
    {
        if (x < right.x)
            return true;
        else if (x > right.x)
            return false;

        if (y < right.y)
            return true;
        else if (y > right.y)
            return false;

        if (z < right.z)
            return true;
        else if (z > right.z)
            return false;

        return false;
     }

