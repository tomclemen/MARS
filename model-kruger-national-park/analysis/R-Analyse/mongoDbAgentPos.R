library(ncdf4)
library(raster)
library(maptools)
library(rgdal)
library(plyr)
library(RColorBrewer)

library(mongolite)
library(jsonlite)

# Connect to mongodb
# will be passed to mongo query
tick <- 5000
agentType <- "Elephant"
collection <- "e1abaaf8-ec2a-4aef-8761-b1f4fcbb57c2-kf"
con <- mongo(collection, url = "mongodb://127.0.0.1:27017/ResultData")

# settings
yearTitlePng <- tick

llBound <- c(30.5,-25.5)
urBound <- c(32.5,-21.5)
cellSizeInDeg <- 0.25

# output/input
png( file=sprintf("./agentdata-18-0.1deg-%s.png",yearTitlePng), height=825, width=595 )
rivers <- shapefile("./gis_vector_rivers/gis_rivers.shp", stringsAsFactors=FALSE, verbose=FALSE, warnPRJ=TRUE)
borders <- shapefile("./gis_raster_border/gis_raster_border.shp", stringsAsFactors=FALSE, verbose=FALSE, warnPRJ=TRUE)
#l <- readOGR(dsn="/Users/thc/ownCloud/03 Land/Clemen et al./R/boundary", layer="boundary")

grid_col <- (urBound[1] - llBound[1]) / cellSizeInDeg
grid_row <- (urBound[2] - llBound[2]) / cellSizeInDeg

# iterate by x,y coords and fill coords matrices
xx <- seq(llBound[1], urBound[1]-cellSizeInDeg, by=cellSizeInDeg)
yy <- seq(llBound[2], urBound[2]-cellSizeInDeg, by=cellSizeInDeg)
print(xx)
print(yy)
aglb1 <- matrix( NA, length(yy), length(xx) )
mx  <- matrix( NA, length(yy), length(xx) )
my  <- matrix( NA, length(yy), length(xx) )

for ( x in 1:grid_col )
{
  for ( y in 1:grid_row )
  {
    mx[y,x]<- xx[x]
    my[y,x]<- yy[y]
    # get data
    aglb1[y,x] <- con$count(sprintf('{"Tick":%i, "Properties.AgentType":"%s", "Position":{ "$geoWithin": { "$box": [[%f,%f],[%f,%f]]  }} }', tick, agentType, xx[x],yy[y],xx[x]+cellSizeInDeg,yy[y]+cellSizeInDeg))
    print(sprintf("%f %f found %i \n",xx[x], yy[y], aglb1[y,x]))
  }
}
test <- aglb1
sum(aglb1)

# from original code
lonlat     <- expand.grid(xx+(cellSizeInDeg/2),yy+(cellSizeInDeg/2)) 

xyz_aglb1   <- data.frame(cbind(lonlat,as.vector(t(aglb1))))
xyz_aglb1_r <- rasterFromXYZ(xyz_aglb1, res=c(cellSizeInDeg,cellSizeInDeg), crs=NA, digits=5)

# ---------------------------------------------------------------------
# --- Plots -----------------------------------------------------------
# ---------------------------------------------------------------------
data(wrld_simpl)

# format output frame (4 cols, smaller inner and outer margin)
par( mfrow=c(1,1), mar=c(3,3,1,4), oma=c(0,2,2,1) )

till<-1500
bm_brk <- seq(2,till, by=till/8)
bm_col <- brewer.pal(length(bm_brk),"Greens")

plot(xyz_aglb1_r, col=bm_col, breaks=bm_brk, main=yearTitlePng ) #
plot(rivers, add=T)
plot(borders, add=T)
#plot(l, add=T)
plot(wrld_simpl,add=T)