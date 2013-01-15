%points = rand(4, 3);
coefs = lls_plane(points);

x = [0:0.1:1];
y = x;
[Xmesh Ymesh] = meshgrid(x, y);

Zplane = - 1 / coefs(3) * (Xmesh .* coefs(1) + Ymesh .* coefs(2) + coefs(4));

plot3(points(:,1), points(:,2),  points(:,3), "color", "red");
surface(Xmesh, Ymesh, Zplane);
colormap("gray");
