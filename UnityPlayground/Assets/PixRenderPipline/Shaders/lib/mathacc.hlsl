#ifndef MATHACC_INCLUDED
#define MATHACC_INCLUDED

#define EPS 1e-5



half fast_cos(half x){
    half v = x + PI;
    v = v/PI;
    half a = floor(v);
    half b = a % 2.0;
    half c = 2*b-1;
    half t = v-a;
    half t2 = t*t;
    half s = b - c*(3*t2 - 2*t*t2);
    return 2*s-1;
}

half fast_sin(half x){
    return fast_cos(x + HALF_PI);
}

void fast_sincos(half x, out half s, out half c){
    c = fast_cos(x);
    s = fast_sin(x);
}

half fast_acos(half x){
    half q = -0.55*x;
    half s = sign(q);
    half a = 1.8h;
    half b = 3;
    half c = 350;

    half q2 = q*q;
    half q4 = q2*q2;
    half q6 = q4*q2;
    half q12 = q6*q6;

    half r = a*q + s*b*q4 + s*c*q12 + HALF_PI;
    return r;
}

half fast_atan2(half y, half x)
{
    const half n1 = 0.97239411f;
    const half n2 = -0.19194795f;
    
    x = abs(x) < EPS ? sign(x) * EPS : x;
    
    half abs_z = abs(y / x);
    
    half result = 0.0;
    if (abs_z <= 1.0) {
        half z2 = abs_z * abs_z;
        result = ((n2 * z2 + n1) * abs_z);
    } else {
        half z2 = 1.0 / (abs_z * abs_z);
        result = HALF_PI - ((n2 * z2 + n1) / abs_z);
    }
    
    if (x < 0.0)
        result = PI - result;
    
    return (y < 0.0) ? -result : result;
}

#endif
