#ifndef MATHACC_INCLUDED
#define MATHACC_INCLUDED

#define EPS 1e-5

half fast_cos(half x){
    half s = sign(x);
    half a = fmod(x , TWO_PI);
    a *= s;
    half b = a * INV_PI - 1;
    half t = abs(b);
    half t2 = t*t;
    half d = -2*t + 3;
    s = t2*d;
    return 2*s - 1;
}

half fast_sin(half x){
    return fast_cos(x - HALF_PI);
}

void fast_sincos(half x, out half s, out half c){
    c = fast_cos(x);
    s = fast_sin(x);
}

half fast_acos(half x){
    half q = -0.55h*x;
    half s = sign(q);

    half q2 = q*q;
    half q4 = q2*q2;
    half q6 = q4*q2;
    half q12 = q6*q6;

    half d = 1.8*q;
    half f = s*q4;
    half h = s*q12;

    half g = 3*f + d;
    half i = 350*h + g;

    return i + HALF_PI;
}

half fast_atan2(half y, half x)
{
    const half n1 = 0.9724h;
    const half n2 = -0.1919h;
    
    x = abs(x) < EPS ? sign(x) * EPS : x;
    
    half abs_z = abs(y / x);
    
    half result = 0.0h;
    if (abs_z <= 1.0h) {
        half z2 = abs_z * abs_z;
        result = ((n2 * z2 + n1) * abs_z);
    } else {
        half z2 = 1.0h / (abs_z * abs_z);
        result = HALF_PI - ((n2 * z2 + n1) / abs_z);
    }
    
    if (x < 0.0h)
        result = PI - result;
    
    return (y < 0.0h) ? -result : result;
}

#endif
