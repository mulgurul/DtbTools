<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="2.0"
    xmlns:dtb="http://www.daisy.org/z3986/2005/dtbook/" 
    xmlns="http://www.daisy.org/z3986/2005/dtbook/"
    exclude-result-prefixes="dtb">
    <xsl:output encoding="utf-8" method="xml" version="1.0" indent="no"
        doctype-public="-//NISO//DTD dtbook 2005-3//EN"
        doctype-system="http://www.daisy.org/z3986/2005/dtbook-2005-3.dtd"/>
    
    <xsl:template match="dtb:level">
        <xsl:choose>
            <xsl:when test="1&lt;=@depth and @depth&lt;=6">
                <xsl:element name="{concat('level', @depth)}">
                    <xsl:apply-templates select="@*[local-name()!='depth']"/>
                    <xsl:apply-templates/>
                </xsl:element>
            </xsl:when>
            <xsl:otherwise>
                <div>
                    <xsl:apply-templates select="@*[local-name()!='depth']"/>
                    <xsl:apply-templates/>
                </div>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    
    <xsl:template match="dtb:hd">
        <xsl:choose>
            <xsl:when test="1&lt;=../@depth and ../@depth&lt;=6">
                <xsl:element name="{concat('h', ../@depth)}">
                    <xsl:apply-templates select="@*"/>
                    <xsl:apply-templates/>
                </xsl:element>
            </xsl:when>
            <xsl:otherwise>
                <bridgehead>
                    <xsl:apply-templates select="@*"/>
                    <xsl:apply-templates/>
                </bridgehead>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    
    <xsl:template match="*">
        <xsl:copy>
            <xsl:copy-of select="@*"/>
            <xsl:apply-templates />
        </xsl:copy>
    </xsl:template>
    <xsl:template match="@*|comment()|text()">
        <xsl:copy />
    </xsl:template>
</xsl:stylesheet>