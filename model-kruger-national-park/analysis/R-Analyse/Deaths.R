library(ncdf4)
library(raster)
library(maptools)
library(rgdal)
library(plyr)
library(RColorBrewer)

library(mongolite)
library(jsonlite)

library(ggplot2)
library(ggspatial)
library(gridExtra)
library(ggpubr)

reasons <- c("NO_WATER", "AGE", "NO_FOOD", "CULLING") #

collectionAgents <- "1411e3f3-3aae-42cd-b778-7fdd3f3a2597-kf"
conAgents <- mongo(collectionAgents, url = "mongodb://127.0.0.1:27017/ResultData")

#https://www.rdocumentation.org/packages/raster/versions/2.6-7/topics/shapefile
rivers <- shapefile("./gis_vector_rivers/gis_rivers.shp", stringsAsFactors=FALSE, verbose=FALSE, warnPRJ=TRUE)
borders <- shapefile("./gis_raster_border/gis_raster_border.shp", stringsAsFactors=FALSE, verbose=FALSE, warnPRJ=TRUE)



# Converting lat/long to DM, DMS see link
# https://stackoverflow.com/questions/45698588/revisiting-the-format-latitude-and-longitude-axis-labels-in-ggplot
scale_x_longitude <- function(xmin=-180, xmax=180, step=1, ...) {
  xbreaks <- seq(xmin,xmax,step)
  xlabels <- unlist(lapply(xbreaks, function(x) ifelse(x < 0, parse(text=paste0(x,"^o", "*W")), ifelse(x > 0, parse(text=paste0(x,"^o", "*E")),x))))
  return(scale_x_continuous("Longitude", breaks = xbreaks, labels = xlabels, expand = c(0, 0)))
}
scale_y_latitude <- function(ymin=-90, ymax=90, step=0.5, ...) {
  ybreaks <- seq(ymin,ymax,step)
  ylabels <- unlist(lapply(ybreaks, function(x) ifelse(x < 0, parse(text=paste0(x,"^o", "*S")), ifelse(x > 0, parse(text=paste0(x,"^o", "*N")),x))))
  return(scale_y_continuous("Latitude", breaks = ybreaks, labels = ylabels, expand = c(0, 0)))
} 


plots <- list()
i<-1
deathsPerTick <- data.frame( "reason" = character(), "tick" = integer(), "count" = integer())
for (reason in reasons) {
  ### agent data
  agentData <- conAgents$find(sprintf('{"Properties.MatterOfDeath":"%s", "Properties.AgentType":"%s"}', reason, "Elephant"), limit=1000000)
  agentDataCount <- nrow(agentData)
  
  print(sprintf("%i agents died because of: %s",agentDataCount,reason))

  xVals = c()
  yVals = c()
  
  if(nrow(agentData) > 0) {
    for(pos in agentData$Position){
      xVals <- append(xVals,pos[1])
      yVals <- append(yVals,pos[2])
      
     # deathsPerTick <- 
    }
    agentDataPos <- data.frame(x=xVals,y=yVals)
    
    plots[[i]] <- ggplot(agentDataPos,aes(x, y)) +
      theme( # plot.margin = unit(c(1, 0, 1, 0), "cm"),
            panel.background = element_blank(),
            text = element_text(size=6), 
            axis.text = element_text(size=6),
            plot.title = element_text(size = 7, face = "bold"),
            legend.title=element_text(size=6), 
            legend.text=element_text(size=6)) + #
      coord_sf(crs = 4326) +
      labs(title = sprintf("Reason: %s \n Agents: %i",reason,agentDataCount)) + #x = "", y = "",
      geom_point(size=0.2,color="gray50")+
      geom_hex(binwidth = c(0.09,0.09), alpha=0.85)+ #10km2 geom_bin2d
      scale_fill_gradient(low = "#ffaabb", high = "#550011") + 
      #stat_density_2d(aes(fill = ..level..), geom = "polygon")
      scale_x_longitude(30.5,32.5,0.5)+
      scale_y_latitude(-25.5,-21.5,0.5)+
      geom_polygon(data=rivers, aes(x=long, y=lat, group=group), 
                   fill=NA,color="lightskyblue", size=0.1)+
      geom_polygon(data=borders, aes(x=long, y=lat, group=group), 
                   fill=NA,color="azure4", size=0.1)
    
    i<-i+1
  }
}


# output/input
#png( file="Deaths.png", height=1754, width=1240, units = "px", res=300 ) #300 print, units = "cm"
# arrange: 
#         http://rstudio-pubs-static.s3.amazonaws.com/2852_379274d7c5734f979e106dcf019ec46c.html
#         http://www.sthda.com/english/wiki/print.php?id=177
#sub last iteration, todo: for loop in R
i <- i - 1
rows <- ceiling(i/2)

g <- ggarrange( plotlist = plots, nrow = rows, ncol = min(i,2)) #, common.legend = TRUE, legend="top"
ggsave( "MatterOfDeathPositions.png", g, width = 14, height = 7*rows, units = "cm", dpi = 300 ) # no automatic height detection!
#dev.off()
